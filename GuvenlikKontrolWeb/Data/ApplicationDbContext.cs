using GuvenlikKontrol.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;


namespace GuvenlikKontrolWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Nokta> Noktalar { get; set; }
    }

}
