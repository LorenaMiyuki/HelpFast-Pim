using System.ComponentModel.DataAnnotations;

namespace WebAppSuporteIA.Models
{
    public class Faq
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Pergunta { get; set; } = string.Empty;
        [Required]
        public string Resposta { get; set; } = string.Empty;
    }
}