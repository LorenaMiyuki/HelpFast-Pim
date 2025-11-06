using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpFast_Pim.Models
{
    [Table("Chats", Schema = "dbo")]
    public class Chat
    {
        [Key]
        public int Id { get; set; }

        // FK para Chamado (opcional: permite associar uma mensagem a um chamado)
        public int? ChamadoId { get; set; }
        [ForeignKey(nameof(ChamadoId))]
        public Chamado? Chamado { get; set; }

        // Remetente / Destinatário (ids e navegações)
        public int RemetenteId { get; set; }
        [ForeignKey(nameof(RemetenteId))]
        public Usuario? Remetente { get; set; }

        public int DestinatarioId { get; set; }
        [ForeignKey(nameof(DestinatarioId))]
        public Usuario? Destinatario { get; set; }

        public string? Mensagem { get; set; }
        public DateTime DataEnvio { get; set; }

        // Tipo de mensagem: "Usuario" ou "Assistente"
        [MaxLength(50)]
        public string? Tipo { get; set; } = "Usuario";

        // Propriedade de alias para compatibilidade com views
        [NotMapped]
        public DateTime DataHora
        {
            get { return DataEnvio; }
            set { DataEnvio = value; }
        }
    }
}