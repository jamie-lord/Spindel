using System;
using System.ComponentModel.DataAnnotations;

namespace Spindel.Models
{
    public class Page
    {
        public int Id { get; set; }
        [Required]
        public string Url { get; set; }
        public DateTime? LastCrawl { get; set; }
    }
}
