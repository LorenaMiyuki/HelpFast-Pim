using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpFast_Pim.Models
{
    [Table("Faqs", Schema = "dbo")]
    public class Faq
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(250)]
        public string? Pergunta { get; set; }

        public string? Resposta { get; set; }

        public bool Ativo { get; set; }
    }
}