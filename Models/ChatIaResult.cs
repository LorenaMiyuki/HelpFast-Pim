using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpFast_Pim.Models
{
    [Table("ChatIaResults", Schema = "dbo")]
    public class ChatIaResult
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Chat")]
        public int ChatId { get; set; }

        // Resposta ou JSON retornado pela IA
        [Required]
        [MaxLength(4000)]
        public string ResultJson { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Chat? Chat { get; set; }
    }
}
