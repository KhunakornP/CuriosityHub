using Microsoft.EntityFrameworkCore;
using CommentService.Apis;
using CommentService.Data;
using CommentService.Interfaces;
using CommentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Point directly at mariadb running locally specifically for running dotnet ef local testing
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Port=3306;Database=commentsdb;User=root;Password=secret;";

builder.Services.AddDbContext<CommentDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<ICommentAppService, CommentAppService>();

var app = builder.Build();

// Auto-migrate on startup for development convenience
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CommentDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Redirect locally to not conflict with internal mappings
app.UseHttpsRedirection();

app.MapCommentEndpoints();

app.Run();
