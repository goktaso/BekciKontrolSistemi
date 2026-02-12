using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace GuvenlikKontrolWeb.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string SecimTipi { get; set; }

        [BindProperty]
        public DateTime BaslangicTarihi { get; set; }

        [BindProperty]
        public DateTime BitisTarihi { get; set; }

        public List<AnalizModel> AnalizSonucu { get; set; } = new();

        public void OnGet()
        {
        }

        public void OnPost()
        {
        }
    }

    public class AnalizModel
    {
        public string Tarih { get; set; }
        public string Bekci { get; set; }
        public int ToplamTur { get; set; }
    }
}
