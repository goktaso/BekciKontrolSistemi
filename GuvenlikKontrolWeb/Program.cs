using System.Data;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// ---------------- Günlük Tur & Tur Analiz API ----------------
app.MapGet("/api/TurAnaliz", async (string baslangic, string bitis, IConfiguration config) =>
{
    var result = new List<object>();
    var connStr = config.GetConnectionString("DefaultConnection");

    try
    {
        using var conn = new SqlConnection(connStr);
        using var cmd = new SqlCommand("sp_TurAnalizi", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("@BasTarih", SqlDbType.DateTime).Value = DateTime.Parse(baslangic);
        cmd.Parameters.Add("@BitTarih", SqlDbType.DateTime).Value = DateTime.Parse(bitis);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new
            {
                SatirTipi = reader["SatirTipi"]?.ToString(),
                OperasyonGunu = reader["OperasyonGunu"],
                BekciAdi = reader["BekciAdi"]?.ToString(),
                KontrolNoktasiAdi = reader["KontrolNoktasiAdi"]?.ToString(),
                DevriyeZamani = reader["DevriyeZamani"],
                TurNo = reader["TurNo"],
                TurIciNo = reader["TurIciNo"],
                IkiNoktaArasiSn = reader["IkiNoktaArasiSn"],
                TurToplamSn = reader["TurToplamSn"],
                IkiTurArasiSn = reader["IkiTurArasiSn"]
            });
        }
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }

    return Results.Ok(result);
});

// ---------------- Eksik Nokta API ----------------
app.MapGet("/api/EksikNokta", async (string baslangic, string bitis, IConfiguration config) =>
{
    var result = new List<object>();
    var connStr = config.GetConnectionString("DefaultConnection");

    try
    {
        using var conn = new SqlConnection(connStr);
        using var cmd = new SqlCommand("sp_EksikNoktaOzet", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("@BasTarih", SqlDbType.DateTime).Value = DateTime.Parse(baslangic);
        cmd.Parameters.Add("@BitTarih", SqlDbType.DateTime).Value = DateTime.Parse(bitis);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new
            {
                OperasyonGunu = reader["OperasyonGunu"],
                BekciAdi = reader["BekciAdi"].ToString(),
                ToplamEksikNokta = Convert.ToInt32(reader["ToplamEksikNokta"]),
                TurNo = reader["TurNo"]
            });
        }
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }

    return Results.Ok(result);
});

// ---------------- Bekçi Performans API ----------------
app.MapGet("/api/BekciPerformans", async (string baslangic, string bitis, IConfiguration config) =>
{
    // Bekçileri gruplamak için bir sözlük (Dictionary) kullanýyoruz
    var bekciOzetleri = new Dictionary<string, dynamic>();
    string connectionString = config.GetConnectionString("DefaultConnection");

    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        string query = "EXEC sp_EksikNoktaOzet @BasTarih, @BitTarih";
        SqlCommand cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@BasTarih", DateTime.Parse(baslangic));
        cmd.Parameters.AddWithValue("@BitTarih", DateTime.Parse(bitis));

        await conn.OpenAsync();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                string ad = reader["BekciAdi"]?.ToString() ?? "Bilinmiyor";
                if (string.IsNullOrWhiteSpace(ad) || ad == "1") continue; // Hatalý verileri ele

                // Deðerleri güvenli bir þekilde al
                int toplam = reader["ToplamTur"] != DBNull.Value ? Convert.ToInt32(reader["ToplamTur"]) : 0;
                int eksikli = reader["EksikliTur"] != DBNull.Value ? Convert.ToInt32(reader["EksikliTur"]) : 0;
                int eksiksiz = reader["EksiksizTur"] != DBNull.Value ? Convert.ToInt32(reader["EksiksizTur"]) : 0;
                int nokta = reader["ToplamEksikNokta"] != DBNull.Value ? Convert.ToInt32(reader["ToplamEksikNokta"]) : 0;

                if (bekciOzetleri.ContainsKey(ad))
                {
                    // Bekçi zaten varsa üzerine ekle (Toplamlarýný al)
                    var mevcut = bekciOzetleri[ad];
                    bekciOzetleri[ad] = new
                    {
                        bekciAdi = ad,
                        toplamTur = mevcut.toplamTur + toplam,
                        eksikTur = mevcut.eksikTur + eksikli,
                        eksiksizTur = mevcut.eksiksizTur + eksiksiz,
                        toplamEksikNokta = mevcut.toplamEksikNokta + nokta
                    };
                }
                else
                {
                    // Bekçi ilk kez geliyorsa ekle
                    bekciOzetleri[ad] = new
                    {
                        bekciAdi = ad,
                        toplamTur = toplam,
                        eksikTur = eksikli,
                        eksiksizTur = eksiksiz,
                        toplamEksikNokta = nokta
                    };
                }
            }
        }
    }

    // Son adýmda Performans Yüzdesini hesapla ve listeye çevir
    var finalResult = bekciOzetleri.Values.Select(b => new {
        b.bekciAdi,
        b.toplamTur,
        b.eksikTur,
        b.eksiksizTur,
        b.toplamEksikNokta,
        performans = b.toplamTur > 0 ? Math.Round(((double)b.eksiksizTur / b.toplamTur) * 100, 1) : 0
    }).OrderByDescending(x => x.performans).ToList();

    return Results.Ok(finalResult);
});
app.Run();
