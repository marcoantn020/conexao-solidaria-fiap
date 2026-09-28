using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Worker.Doacoes.CampanhasApi;
using Worker.Doacoes.Consumo;
using Worker.Doacoes.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DoacoesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DoacoesDb")));

builder.Services.Configure<CampanhasApiOptions>(builder.Configuration.GetSection("CampanhasApi"));
builder.Services.AddHttpClient<ICampanhasApiClient, CampanhasApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<CampanhasApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddScoped<DoacaoProcessor>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddHostedService<DoacaoConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DoacoesDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }
