using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpFast_Pim.Models
{
    [Table("Cargos", Schema = "dbo")]
    public class Cargo
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string? Nome { get; set; }

        public ICollection<Usuario>? Usuarios { get; set; } = new List<Usuario>();
    }
}