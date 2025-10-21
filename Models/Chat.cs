using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppSuporteIA.Models
{
    public class Chat
    {
        [Key]
        public int Id { get; set; }
        public int ChamadoId { get; set; }
        [Required]
        public string Mensagem { get; set; } = string.Empty;
        public bool EnviadoPorCliente { get; set; }
        public DateTime DataEnvio { get; set; }
        public Chamado Chamado { get; set; } = null!;
    }
}