using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppSuporteIA.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Nome { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Senha { get; set; } = string.Empty;
        [Required]
        public string Telefone { get; set; } = string.Empty;
        public DateTime? UltimoLogin { get; set; }
        public int CargoId { get; set; }
        [ForeignKey("CargoId")]
        public Cargo Cargo { get; set; } = null!;
    }
}