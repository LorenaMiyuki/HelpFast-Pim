using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebAppSuporteIA.Models
{
    public class Cargo
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Nome { get; set; } = string.Empty;

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}