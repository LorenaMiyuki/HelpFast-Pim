using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppSuporteIA.Models
{
    public class Chamado
    {
        [Key]
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public int? TecnicoId { get; set; }
        [Required]
        public string Motivo { get; set; } = string.Empty;
        [Required]
        public string Status { get; set; } = string.Empty;
        public DateTime DataAbertura { get; set; }
        public DateTime? DataFechamento { get; set; }
        [ForeignKey("ClienteId")]
        public Usuario Cliente { get; set; } = null!;
        [ForeignKey("TecnicoId")]
        public Usuario? Tecnico { get; set; }
    }
}