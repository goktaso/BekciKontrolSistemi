using System.Data;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// ------------------ Services ------------------
builder.Services.AddRazorPages();

// ------------------ App Build ------------------
var app = builder.Build();

// ------------------ Middleware ------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Razor Pages
app.MapRazorPages();

// ------------------ WebAPI Endpoint ------------------
app.MapGet("/api/TurAnaliz", async (string baslangic, string bitis, IConfiguration config) =>
{
    var result = new List<Dictionary<string, object>>();
    var connectionString = config.GetConnectionString("DefaultConnection");

    try
    {
        using var conn = new SqlConnection(connectionString);
        using var cmd = new SqlCommand("sp_TurAnalizi", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        // SP’nin beklediði parametre isimleri
        cmd.Parameters.AddWithValue("@BasTarih", DateTime.Parse(baslangic));
        cmd.Parameters.AddWithValue("@BitTarih", DateTime.Parse(bitis));

        await conn.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.GetValue(i);

            result.Add(row);
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    return Results.Ok(result);
});

// ------------------ Run App ------------------
app.Run();
