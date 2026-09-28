using Api.CampanhasUsuarios.Auth;
using Api.CampanhasUsuarios.Data;
using Api.CampanhasUsuarios.Doacoes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<CampanhasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CampanhasDb")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

builder.Services.Configure<InternalOptions>(builder.Configuration.GetSection("Internal"));

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CampanhasDbContext>();
    db.Database.EnsureCreated();

    if (!db.GestorOngs.Any())
    {
        db.GestorOngs.Add(new GestorOng
        {
            Id = Guid.NewGuid(),
            Nome = "Administrador ONG",
            Email = "gestor@esperancasolidaria.org",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("Admin123!")
        });
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapScalarApiReference(options =>
{
    options.OpenApiRoutePattern = "/swagger/{documentName}/swagger.json";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }
