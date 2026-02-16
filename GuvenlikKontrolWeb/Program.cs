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
    var result = new List<object>();
    var connectionString = config.GetConnectionString("DefaultConnection");

    try
    {
        using var conn = new SqlConnection(connectionString);
        using var cmd = new SqlCommand("sp_TurAnalizi", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add("@BasTarih", SqlDbType.DateTime)
            .Value = DateTime.Parse(baslangic);

        cmd.Parameters.Add("@BitTarih", SqlDbType.DateTime)
            .Value = DateTime.Parse(bitis);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new
            {
                satirTipi = reader["SatirTipi"]?.ToString(),
                operasyonGunu = reader["OperasyonGunu"],
                bekciAdi = reader["BekciAdi"]?.ToString(),
                kontrolNoktasiAdi = reader["KontrolNoktasiAdi"]?.ToString(),
                devriyeZamani = reader["DevriyeZamani"],
                turNo = reader["TurNo"],
                turIciNo = reader["TurIciNo"],
                ikiNoktaArasiSn = reader["IkiNoktaArasiSn"],
                turToplamSn = reader["TurToplamSn"],
                ikiTurArasiSn = reader["IkiTurArasiSn"]
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
