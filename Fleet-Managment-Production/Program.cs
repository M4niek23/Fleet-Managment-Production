using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentity<AppUser,IdentityRole>(
    opitions =>
    {
        opitions.Password.RequiredUniqueChars = 0;
        opitions.Password.RequireUppercase = false;
        opitions.Password.RequiredLength = 6;
        opitions.Password.RequireNonAlphanumeric = false;
        opitions.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<ApDbContext>().AddDefaultTokenProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
