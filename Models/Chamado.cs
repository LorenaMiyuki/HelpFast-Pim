using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpFast_Pim.Models
{
    [Table("Chamados", Schema = "dbo")]
    public class Chamado
    {
        [Key]
        public int Id { get; set; }

        // Campo apenas para UI (não mapeado no banco)
        [NotMapped]
        public string? Assunto { get; set; }

        public string? Motivo { get; set; }

        // FK para Cliente (usuario que abriu)
        public int ClienteId { get; set; }
        [ForeignKey(nameof(ClienteId))]
        public Usuario? Cliente { get; set; }

        // FK para Técnico (pode ser nulo até ser atribuído)
        public int? TecnicoId { get; set; }
        [ForeignKey(nameof(TecnicoId))]
        public Usuario? Tecnico { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        public DateTime? DataAbertura { get; set; }
        public DateTime? DataFechamento { get; set; }

        // Navegação para Chats
        public ICollection<Chat> Chats { get; set; } = new List<Chat>();
    }
}