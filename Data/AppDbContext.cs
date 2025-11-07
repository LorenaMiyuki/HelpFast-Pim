using Microsoft.EntityFrameworkCore;
using HelpFast_Pim.Models;

namespace HelpFast_Pim.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<Chamado> Chamados { get; set; }
        public DbSet<HistoricoChamado> HistoricoChamados { get; set; }
        public DbSet<Faq> Faqs { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatIaResult> ChatIaResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<Cargo>().ToTable("Cargos");
            modelBuilder.Entity<Chamado>().ToTable("Chamados");
            // Índices de desempenho para consultas frequentes sobre Chamados
            modelBuilder.Entity<Chamado>(entity =>
            {
                entity.HasIndex(c => c.ClienteId).HasDatabaseName("IX_Chamados_ClienteId");
                entity.HasIndex(c => c.TecnicoId).HasDatabaseName("IX_Chamados_TecnicoId");
                entity.HasIndex(c => c.Status).HasDatabaseName("IX_Chamados_Status");
                entity.HasIndex(c => new { c.ClienteId, c.Status, c.DataAbertura }).HasDatabaseName("IX_Chamados_Cliente_Status_Data");
                entity.HasIndex(c => new { c.TecnicoId, c.Status, c.DataAbertura }).HasDatabaseName("IX_Chamados_Tecnico_Status_Data");
            });
            modelBuilder.Entity<HistoricoChamado>().ToTable("HistoricoChamados");
            modelBuilder.Entity<Faq>().ToTable("Faqs");
            modelBuilder.Entity<Chat>().ToTable("Chats");
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

            modelBuilder.Entity<HistoricoChamado>(entity =>
            {
                entity.ToTable("HistoricoChamados");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Acao).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Data).IsRequired();

                entity.HasOne(e => e.Chamado)
                    .WithMany()
                    .HasForeignKey(e => e.ChamadoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Mensagem)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.Property(e => e.DataEnvio)
                    .IsRequired();

                entity.HasOne(e => e.Chamado)
                    .WithMany(c => c.Chats)
                    .HasForeignKey(e => e.ChamadoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Remetente)
                    .WithMany()
                    .HasForeignKey(e => e.RemetenteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Destinatario)
                    .WithMany()
                    .HasForeignKey(e => e.DestinatarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                    // Índice para acelerar listagem cronológica por chamado
                    entity.HasIndex(e => new { e.ChamadoId, e.DataEnvio }).HasDatabaseName("IX_Chats_Chamado_DataEnvio");
            });

            modelBuilder.Entity<Faq>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Pergunta)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Resposta)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

            modelBuilder.Entity<ChatIaResult>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ResultJson)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.HasOne(e => e.Chat)
                    .WithMany()
                    .HasForeignKey(e => e.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
