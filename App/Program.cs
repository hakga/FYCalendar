using Microsoft.EntityFrameworkCore;
using App.DB;
using App.Domain.Repositories;
using App.Infrastructures;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SampleDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<ILoaderMembers, DBLoaderMembers>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error");}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute( name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
