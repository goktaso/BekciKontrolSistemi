using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YourProject.Pages
{
    public class AtilmayanTurModel : PageModel
    {
        // Veri listesi buraya gelecek (Örn: List<Tur> Turlar)

        public void OnGet()
        {
            // Sayfa yüklendiðinde çalýþacak kodlar
        }

        // Excel dýþa aktarma iþlemi için handler
        public IActionResult OnPostExportExcel()
        {
            // Excel oluþturma mantýðý
            return File(new byte[0], "application/vnd.ms-excel", "AtilmayanTurlar.xlsx");
        }
    }
}