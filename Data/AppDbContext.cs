
using Microsoft.EntityFrameworkCore;
using WebAppSuporteIA.Models;

namespace WebAppSuporteIA.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<Chamado> Chamados { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Faq> Faqs { get; set; }
        public DbSet<HistoricoChamado> HistoricoChamados { get; set; }
        public DbSet<ChatIaResult> ChatIaResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Usuario>().ToTable("Usuarios");
        modelBuilder.Entity<Cargo>().ToTable("Cargos");
        modelBuilder.Entity<Chamado>().ToTable("Chamados");
        modelBuilder.Entity<Chat>().ToTable("Chats");
        modelBuilder.Entity<Faq>().ToTable("Faqs");
        modelBuilder.Entity<HistoricoChamado>().ToTable("HistoricoChamados");
        modelBuilder.Entity<ChatIaResult>().ToTable("ChatIaResults");

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Telefone).IsRequired().HasMaxLength(15);
            entity.Property(e => e.Senha).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(e => e.Cargo)
                .WithMany(c => c.Usuarios)
                .HasForeignKey(e => e.CargoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cargo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Nome).IsUnique();
        });

        // Remover configuração inválida de HistoricoChamado
        modelBuilder.Entity<HistoricoChamado>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Acao).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Data).IsRequired();
            entity.HasOne(e => e.Chamado)
                .WithMany()
                .HasForeignKey(e => e.ChamadoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Dados iniciais para teste
        modelBuilder.Entity<Cargo>().HasData(
            new Cargo { Id = 1, Nome = "Administrador" },
            new Cargo { Id = 2, Nome = "Tecnico" },
            new Cargo { Id = 3, Nome = "Cliente" }
        );

        // Hash da senha "123456" em Base64
        var senhaHash = "jZae72+kaOmKs9oGzww/XVgGfQDUZbZVQYhU5h0hQqk=";

        modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = 1,
                Nome = "Administrador Master",
                Email = "admin@helpfast.com",
                Telefone = "(11) 99999-9999",
                Senha = senhaHash,
                CargoId = 1 // Cargo "Administrador"
            }
        );
    }
}