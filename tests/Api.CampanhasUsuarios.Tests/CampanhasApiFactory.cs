using System.Linq;
using Api.CampanhasUsuarios.Data;
using Api.CampanhasUsuarios.Doacoes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.CampanhasUsuarios.Tests;

public class CampanhasApiFactory : WebApplicationFactory<Program>
{
    public const string InternalSharedSecret = "segredo-de-teste-internal-campanhas";

    private readonly string _databaseName = $"CampanhasDb-{Guid.NewGuid()}";

    public FakeEventPublisher EventPublisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CampanhasDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<CampanhasDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            var publisherDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventPublisher));
            if (publisherDescriptor is not null)
                services.Remove(publisherDescriptor);

            services.AddSingleton<IEventPublisher>(EventPublisher);

            services.PostConfigure<InternalOptions>(options => options.SharedSecret = InternalSharedSecret);
        });
    }
}
