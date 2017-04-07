using NanoApi.JsonFile;
using System;

namespace Spindel.Models
{
    public class Page
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Uri { get; set; }
        public DateTime? LastCrawl { get; set; }
    }
}