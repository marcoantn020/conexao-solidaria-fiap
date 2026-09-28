using Api.CampanhasUsuarios.Auth;
using Api.CampanhasUsuarios.Campanhas;
using Api.CampanhasUsuarios.Doacoes;
using Api.CampanhasUsuarios.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Api.CampanhasUsuarios.Data;

public class CampanhasDbContext : DbContext
{
    public CampanhasDbContext(DbContextOptions<CampanhasDbContext> options) : base(options) { }

    public DbSet<Doador> Doadores => Set<Doador>();
    public DbSet<GestorOng> GestorOngs => Set<GestorOng>();
    public DbSet<Campanha> Campanhas => Set<Campanha>();
    public DbSet<CallbackProcessado> CallbacksProcessados => Set<CallbackProcessado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Doador>(entity =>
        {
            entity.HasIndex(d => d.Email).IsUnique();
            entity.Property(d => d.NomeCompleto).IsRequired();
            entity.Property(d => d.Email).IsRequired();
            entity.Property(d => d.Cpf).IsRequired();
            entity.Property(d => d.SenhaHash).IsRequired();
        });

        modelBuilder.Entity<GestorOng>(entity =>
        {
            entity.HasIndex(g => g.Email).IsUnique();
            entity.Property(g => g.Nome).IsRequired();
            entity.Property(g => g.Email).IsRequired();
            entity.Property(g => g.SenhaHash).IsRequired();
        });

        modelBuilder.Entity<Campanha>(entity =>
        {
            entity.Property(c => c.Titulo).IsRequired();
            entity.Property(c => c.MetaFinanceira).HasColumnType("decimal(18,2)");
            entity.Property(c => c.ValorArrecadado).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<CallbackProcessado>(entity =>
        {
            entity.HasKey(c => c.IdDoacao);
        });
    }
}
