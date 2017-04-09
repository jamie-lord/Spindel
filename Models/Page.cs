using System;

namespace Spindel.Models
{
	public class Page
	{
		public string id { get; set; }
		public string label { get; set; }
		public DateTime? lastCrawl { get; set; }
		public int x = 0;
		public int y = 0;
		public int size = 2;
	}
}