using NanoApi.JsonFile;

namespace Spindel.Models
{
    public class Relationship
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int Parent { get; set; }
        public int Child { get; set; }
    }
}
