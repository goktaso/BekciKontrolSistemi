using System.Data;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// ---------------- WebAPI Endpoint ----------------
app.MapGet("/api/TurAnaliz", async (string baslangic, string bitis, IConfiguration config) =>
{
    var result = new List<dynamic>();
    var connectionString = config.GetConnectionString("DefaultConnection");

    try
    {
        using var conn = new SqlConnection(connectionString);
        using var cmd = new SqlCommand("sp_TurAnalizi", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@BasTarih", DateTime.Parse(baslangic));
        cmd.Parameters.AddWithValue("@BitTarih", DateTime.Parse(bitis));

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new
            {
                OperasyonGunu = reader.GetDateTime(0).ToString("yyyy-MM-ddTHH:mm:ss"),
                BekciAdi = reader.GetString(1),
                KontrolNoktasiAdi = reader.GetString(2),
                // BigInt veya sayýsal tipleri önce GetValue() ile alýp string’e çeviriyoruz
                OkuyucuKodu = reader.GetValue(3)?.ToString() ?? "",
                DevriyeZamani = reader.GetDateTime(4).ToString("yyyy-MM-ddTHH:mm:ss")
            });
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    return Results.Ok(result);
});

app.Run();
