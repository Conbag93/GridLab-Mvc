using GridLab.Grid;
using GridLab.HandRolled.Data;
using GridLab.Shared;
using GridLab.Shared.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();   // serves the grid library's CSS from _content/ in any environment
builder.Services.AddControllersWithViews();
builder.Services.AddGridLabData();
builder.Services.AddSingleton(typeof(IGridStore<>), typeof(RepositoryGridStore<>)); // default: let the grid page an IQueryable
builder.Services.AddSingleton<IGridStore<User>, UserDirectoryGridStore>();            // Users: the data layer pages for us

var app = builder.Build();
app.UseStaticFiles();
app.MapControllerRoute("default", "{controller=Products}/{action=Index}/{id?}");
app.Run();
