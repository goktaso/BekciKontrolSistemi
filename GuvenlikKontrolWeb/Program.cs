using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using GuvenlikKontrolWeb.Data;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabaný
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Razor Pages Ayarlarý (KATILAÞTIRILDI)
builder.Services.AddRazorPages(options => {
    options.Conventions.AuthorizeFolder("/"); // Her yer kilitli
    options.Conventions.AllowAnonymousToPage("/Account/Login"); // Sadece Login açýk
});

// 3. Kimlik Doðrulama
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options => {
        options.Cookie.Name = "GuvenlikKontrol.Auth";
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true; // Hareket varsa süreyi uzat
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 4. Middleware Sýralamasý
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// -----------------------------------------------------------
// 5. API UÇLARI
// -----------------------------------------------------------

// TUR ANALÝZ API
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
        return Results.Ok(result);
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
}).RequireAuthorization();

// BEKÇÝ PERFORMANS API (YENÝ EKLENDÝ)
app.MapGet("/api/BekciPerformans", async (string baslangic, string bitis, IConfiguration config) =>
{
    var result = new List<object>();
    var connStr = config.GetConnectionString("DefaultConnection");
    try
    {
        using var conn = new SqlConnection(connStr);
        // SQL'deki Stored Procedure isminin sp_BekciPerformansi olduðundan emin ol
        using var cmd = new SqlCommand("sp_BekciPerformansi", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("@BasTarih", SqlDbType.DateTime).Value = DateTime.Parse(baslangic);
        cmd.Parameters.Add("@BitTarih", SqlDbType.DateTime).Value = DateTime.Parse(bitis);
        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new
            {
                BekciAdi = reader["BekciAdi"]?.ToString(),
                ToplamTur = reader["ToplamTur"],
                EksiksizTur = reader["EksiksizTur"],
                EksikliTur = reader["EksikliTur"],
                EksikNokta = reader["EksikNokta"],
                BasariOrani = reader["BasariOrani"]
            });
        }
        return Results.Ok(result);
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
}).RequireAuthorization();

app.Run();