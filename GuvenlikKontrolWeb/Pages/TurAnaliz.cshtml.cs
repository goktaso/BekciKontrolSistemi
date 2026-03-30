using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Microsoft.Data.SqlClient;

namespace GuvenlikKontrolWeb.Pages
{
    public class TurAnalizModel : PageModel
    {
        private readonly IConfiguration _config;
        public TurAnalizModel(IConfiguration config)
        {
            _config = config;
        }

        public List<TurAnalizItem> TurAnalizData { get; set; } = new List<TurAnalizItem>();

        [BindProperty(SupportsGet = true)]
        public string Baslangic { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Bitis { get; set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrEmpty(Baslangic) || string.IsNullOrEmpty(Bitis))
                return;

            var connStr = _config.GetConnectionString("DefaultConnection");

            try
            {
                using var conn = new SqlConnection(connStr);
                using var cmd = new SqlCommand("sp_TurAnalizi", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@BasTarih", DateTime.Parse(Baslangic));
                cmd.Parameters.AddWithValue("@BitTarih", DateTime.Parse(Bitis));

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // Ýndeksleri SQL SELECT sýrasýna göre düzelttik
                    TurAnalizData.Add(new TurAnalizItem
                    {
                        SatirTipi = reader["SatirTipi"].ToString(),
                        BekciAdi = reader["BekciAdi"].ToString(),
                        KontrolNoktasiAdi = reader["KontrolNoktasiAdi"].ToString(),
                        OperasyonGunu = Convert.ToDateTime(reader["OperasyonGunu"]),
                        DevriyeZamani = Convert.ToDateTime(reader["DevriyeZamani"]),
                        TurNo = Convert.ToInt32(reader["TurNo"]),
                        TurIciNo = Convert.ToInt32(reader["TurIciNo"]),
                        IkiNoktaArasiSn = Convert.ToInt32(reader["IkiNoktaArasiSn"]),
                        TurToplamSn = Convert.ToInt32(reader["TurToplamSn"]),
                        IkiTurArasiSn = Convert.ToInt32(reader["IkiTurArasiSn"])
                    });
                }
            }
            catch (Exception ex)
            {
                // Hata mesajýný görmek için geçici olarak buraya log ekleyebilirsiniz
                // throw ex; 
            }
        }
    }

    public class TurAnalizItem
    {
        public string SatirTipi { get; set; } // DETAY mi TOPLAM mi?
        public string BekciAdi { get; set; }
        public string KontrolNoktasiAdi { get; set; }
        public DateTime OperasyonGunu { get; set; }
        public DateTime DevriyeZamani { get; set; }
        public int TurNo { get; set; }
        public int TurIciNo { get; set; }
        public int IkiNoktaArasiSn { get; set; }
        public int TurToplamSn { get; set; }
        public int IkiTurArasiSn { get; set; }
    }
}