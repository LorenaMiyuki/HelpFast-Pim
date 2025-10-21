using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppSuporteIA.Models
{
    public class HistoricoChamado
    {
        [Key]
        public int Id { get; set; }
        public int ChamadoId { get; set; }
        [Required]
        public string Acao { get; set; } = string.Empty;
        public DateTime Data { get; set; }
        [ForeignKey("ChamadoId")]
        public Chamado Chamado { get; set; } = null!;
    }
}