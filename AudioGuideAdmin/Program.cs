using Microsoft.EntityFrameworkCore;
using AudioGuideAdmin.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Poi}/{action=Index}/{id?}");
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value.ToLower();

    // Cho phép vào login/register
    if (path.StartsWith("/auth"))
    {
        await next();
        return;
    }

    // Check login
    var user = context.Session.GetString("user");

    if (user == null)
    {
        context.Response.Redirect("/Auth/Login");
        return;
    }

    await next();
});
app.Run();