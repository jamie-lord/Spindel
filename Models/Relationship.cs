using System.ComponentModel.DataAnnotations;

namespace Spindel.Models
{
    public class Relationship
    {
        public int Id { get; set; }
        [Key]
        [Required]
        public Page Parent { get; set; }
        [Key]
        [Required]
        public Page Child { get; set; }
    }
}
