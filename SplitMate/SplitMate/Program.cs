
using SplitMate.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var dbFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dbFolder);

var dbPath = Path.Combine(dbFolder, "SplitMate.db");
var connectionString = $"Data Source={dbPath}";

builder.Services.AddSingleton<IDatabase>(
    _ => Database.Initialize(connectionString));

builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
