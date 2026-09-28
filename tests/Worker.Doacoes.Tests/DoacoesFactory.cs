using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Worker.Doacoes.Consumo;
using Worker.Doacoes.Data;

namespace Worker.Doacoes.Tests;

public class DoacoesFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"DoacoesDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DoacoesDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<DoacoesDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            var hostedServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(DoacaoConsumer));
            if (hostedServiceDescriptor is not null)
                services.Remove(hostedServiceDescriptor);
        });
    }
}
