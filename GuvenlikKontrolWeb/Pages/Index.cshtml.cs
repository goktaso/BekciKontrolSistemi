using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace GuvenlikKontrolWeb.Pages
{
    public class IndexModel : PageModel
    {
        public List<Nokta> NoktaListesi { get; set; } = new List<Nokta>();

        public void OnGet()
        {
            string connectionString = "Server=OZAY\\DATA;Database=GuvenlikKontrol;User Id=data;Password=data1234;TrustServerCertificate=True;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = "SELECT NoktaID, NoktaAdi FROM NoktaTanimlari";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        NoktaListesi.Add(new Nokta
                        {
                            NoktaID = dr.GetInt32(0),
                            NoktaAdi = dr.GetString(1)
                        });
                    }
                }
            }
        }
    }

    public class Nokta
    {
        public int NoktaID { get; set; }
        public string NoktaAdi { get; set; }
    }
}
