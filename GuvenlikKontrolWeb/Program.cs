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
        await conn.OpenAsync();

        using var cmd = new SqlCommand("sp_TurAnalizi", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@BasTarih", DateTime.Parse(baslangic + " 00:00:00"));
        cmd.Parameters.AddWithValue("@BitTarih", DateTime.Parse(bitis + " 23:59:59"));

        using var reader = await cmd.ExecuteReaderAsync();
        var rawData = new List<dynamic>();

        while (await reader.ReadAsync())
        {
            DateTime operasyonGunu = reader.IsDBNull(0) ? DateTime.MinValue : reader.GetDateTime(0);
            string bekciAdi = reader.IsDBNull(1) ? "" : reader.GetString(1);
            string kontrolNoktasi = reader.IsDBNull(2) ? "" : reader.GetString(2);
            string okuyucuKodu = reader.IsDBNull(3) ? "" : reader.GetString(3);
            DateTime devriyeZamani = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4);

            rawData.Add(new
            {
                OperasyonGunu = operasyonGunu,
                BekciAdi = bekciAdi,
                KontrolNoktasiAdi = kontrolNoktasi,
                OkuyucuKodu = okuyucuKodu,
                DevriyeZamani = devriyeZamani
            });
        }

        // ---------- Tur analizi ----------
        int turNo = 0;
        int turIciNo = 0;
        string oncekiBekci = null;
        DateTime? oncekiSaat = null;
        int turToplamSn = 0;
        var dictNokta = new Dictionary<string, int>();

        foreach (var row in rawData)
        {
            bool yeniTur = false;
            if (turNo == 0 || (oncekiBekci != null && oncekiBekci != row.BekciAdi))
                yeniTur = true;
            if (turIciNo >= 15)
                yeniTur = true;
            if (dictNokta.ContainsKey(row.KontrolNoktasiAdi) && dictNokta[row.KontrolNoktasiAdi] >= 2)
                yeniTur = true;

            if (yeniTur && turIciNo > 0)
            {
                result.Add(new Dictionary<string, object>
                {
                    ["Tarih"] = "",
                    ["TurNo"] = turNo,
                    ["No"] = "",
                    ["Bekci"] = "",
                    ["KontrolNoktasi"] = $"Tur {turNo} Toplam",
                    ["DevriyeSaati"] = "",
                    ["IkiNoktaArasi"] = TimeSpan.FromSeconds(turToplamSn).ToString(@"hh\:mm\:ss"),
                    ["Dk"] = turToplamSn / 60,
                    ["Sn"] = turToplamSn % 60,
                    ["IkiTurArasi"] = "",
                    ["Uyari"] = "",
                    ["IsTotal"] = true
                });

                turToplamSn = 0;
                dictNokta.Clear();
                turIciNo = 0;
            }

            if (yeniTur) turNo++;

            turIciNo++;
            oncekiBekci = row.BekciAdi;

            if (dictNokta.ContainsKey(row.KontrolNoktasiAdi))
                dictNokta[row.KontrolNoktasiAdi]++;
            else
                dictNokta[row.KontrolNoktasiAdi] = 1;

            int farkSn = 0;
            if (oncekiSaat != null)
            {
                farkSn = (int)(row.DevriyeZamani - oncekiSaat.Value).TotalSeconds;
                if (farkSn < 0) farkSn = 0;
                turToplamSn += farkSn;
            }

            oncekiSaat = row.DevriyeZamani;

            result.Add(new Dictionary<string, object>
            {
                ["Tarih"] = row.OperasyonGunu == DateTime.MinValue ? "" : row.OperasyonGunu.ToString("dd.MM.yyyy"),
                ["TurNo"] = turNo,
                ["No"] = turIciNo,
                ["Bekci"] = row.BekciAdi,
                ["KontrolNoktasi"] = row.KontrolNoktasiAdi,
                ["DevriyeSaati"] = row.DevriyeZamani == DateTime.MinValue ? "" : row.DevriyeZamani.ToString("dd.MM.yyyy HH:mm"),
                ["IkiNoktaArasi"] = TimeSpan.FromSeconds(farkSn).ToString(@"hh\:mm\:ss"),
                ["Dk"] = farkSn / 60,
                ["Sn"] = farkSn % 60,
                ["IkiTurArasi"] = "",
                ["Uyari"] = "",
                ["IsTotal"] = false
            });
        }

        if (turIciNo > 0)
        {
            result.Add(new Dictionary<string, object>
            {
                ["Tarih"] = "",
                ["TurNo"] = turNo,
                ["No"] = "",
                ["Bekci"] = "",
                ["KontrolNoktasi"] = $"Tur {turNo} Toplam",
                ["DevriyeSaati"] = "",
                ["IkiNoktaArasi"] = TimeSpan.FromSeconds(turToplamSn).ToString(@"hh\:mm\:ss"),
                ["Dk"] = turToplamSn / 60,
                ["Sn"] = turToplamSn % 60,
                ["IkiTurArasi"] = "",
                ["Uyari"] = "",
                ["IsTotal"] = true
            });
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
