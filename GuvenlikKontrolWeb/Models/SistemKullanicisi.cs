using System.ComponentModel.DataAnnotations;

namespace GuvenlikKontrolWeb.Models
{
    public class SistemKullanicisi
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string KullaniciAdi { get; set; }

        [Required]
        public string Sifre { get; set; }

        public string? AdSoyad { get; set; }

        public string? Rol { get; set; } // Admin, Bekci vb.
    }
}