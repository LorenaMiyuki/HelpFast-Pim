using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpFast_Pim.Models
{
    [Table("Usuarios", Schema = "dbo")]
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(150)]
        public string? Nome { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        // Armazena o hash da senha
        public string? Senha { get; set; }

        // FK para Cargo
        public int CargoId { get; set; }

        // Navegação (opcional)
        public Cargo? Cargo { get; set; }

        [MaxLength(50)]
        [RegularExpression(@"^\(\d{2}\)\s?\d{4,5}-\d{4}$", ErrorMessage = "Telefone inválido. Use (XX)XXXXX-XXXX ou (XX)XXXX-XXXX")]
        public string? Telefone { get; set; }

        public DateTime? UltimoLogin { get; set; }
    }
}