using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpFast_Pim.Models
{
    [Table("HistoricoChamados", Schema = "dbo")]
    public class HistoricoChamado
    {
        [Key]
        public int Id { get; set; }

        public int ChamadoId { get; set; }
        [ForeignKey(nameof(ChamadoId))]
        public Chamado? Chamado { get; set; }

        [MaxLength(255)]
        public string? Acao { get; set; }

        public DateTime Data { get; set; }

        public int UsuarioId { get; set; }
    }
}