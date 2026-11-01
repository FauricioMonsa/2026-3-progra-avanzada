using Microsoft.EntityFrameworkCore;
using Quiniela.BusinessLogic;
using Quiniela.Model;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string connectionString = builder.Configuration.GetConnectionString("QuinielaDb")
    ?? builder.Configuration.GetConnectionString("MySecretPostgresConnection")
    ?? throw new InvalidOperationException(
        "Set the PostgreSQL connection string using ConnectionStrings:QuinielaDb or ConnectionStrings:MySecretPostgresConnection.");

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<QuinielaDbContext>(options => options.UseNpgsql(
    connectionString,
    npgsql => npgsql
        .MapEnum<MatchKind>("match_kind", "user_08")
        .MapEnum<MatchStatus>("match_status", "user_08")
        .MapEnum<SportType>("sport_type", "user_08")));
builder.Services.AddScoped<QuinielaScorer>();
builder.Services.AddScoped<MatchCrudService>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Quiniela/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Quiniela}/{action=Index}/{id?}");

app.Run();
