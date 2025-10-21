using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppSuporteIA.Models
{

    public class ChatIaResult
    {

        [Key]

        public int Id { get; set; }
        public bool Resolvido { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }
}