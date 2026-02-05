using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using YggdrasilWebApp.Contexts;
using YggdrasilWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Forwarded headers для nginx ----------
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	// говорим asp.net, что мы доверяем X-Forwarded-For и X-Forwarded-Proto
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

	// nginx сидит на этой же машине и шлёт запросы с 127.0.0.1
	options.KnownProxies.Add(IPAddress.Loopback);
});

// ---------- MVC + сервисы ----------
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(opt =>
	opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<TexturesService>();
builder.Services.AddSingleton<TokenGenerator>();

builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(opt =>
	{
		opt.LoginPath = "/account/login";
		opt.AccessDeniedPath = "/account/login";
		opt.Cookie.Name = "ygg_web_auth";
		opt.SlidingExpiration = true;
		opt.ExpireTimeSpan = TimeSpan.FromDays(30);
	});

builder.Services.AddAuthorization();

var app = builder.Build();

// ---------- ПЕРВЫМ ДЕЛОМ: доверяем заголовкам от nginx ----------
app.UseForwardedHeaders();

// ---------- Продакшн-ошибки + HSTS ----------
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

// ВАЖНО: не надо UseHttpsRedirection,
// потому что HTTPS уже терминируется на nginx,
// а Kestrel слушает только HTTP (5002).
// Иначе он сам начнет пытаться редиректить на https, но не знает порт.
// app.UseHttpsRedirection();

{
	var modsDir = Path.Combine(app.Environment.ContentRootPath, "Storage", "Mods");
	Directory.CreateDirectory(modsDir);
}

{
	var modsDir = Path.Combine(app.Environment.ContentRootPath, "Storage", "PonySkins");
	Directory.CreateDirectory(modsDir);
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ГЛОБАЛЬНЫЙ RequireAuthorization СНИМАЕМ,
// иначе /authserver тоже будет требовать куку и редиректить на /account/login.
// Авторизацию вешай атрибутами [Authorize] на нужные контроллеры/экшены.
app.MapControllerRoute(
		name: "default",
		pattern: "{controller=Home}/{action=Index}/{id?}");
//   .RequireAuthorization(); // <- УБРАЛИ

app.Run();
