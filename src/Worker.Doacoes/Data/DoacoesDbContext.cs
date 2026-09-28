using Microsoft.EntityFrameworkCore;
using Worker.Doacoes.Doacoes;

namespace Worker.Doacoes.Data;

public class DoacoesDbContext : DbContext
{
    public DoacoesDbContext(DbContextOptions<DoacoesDbContext> options) : base(options) { }

    public DbSet<Doacao> Doacoes => Set<Doacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Doacao>(entity =>
        {
            entity.HasKey(d => d.IdDoacao);
            entity.Property(d => d.ValorDoacao).HasColumnType("decimal(18,2)");
        });
    }
}
