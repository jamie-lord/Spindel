using Abot.Crawler;
using Abot.Poco;
using Spindel.Models;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Spindel
{
	public class Program
	{
		private static PageConcurrentBag _pages = new PageConcurrentBag();
		private static RelationshipConcurrentBag _relationships = new RelationshipConcurrentBag();
		public static void Main(string[] args)
		{
			PoliteWebCrawler crawler = new PoliteWebCrawler();
			crawler.PageCrawlCompletedAsync += Crawler_ProcessPageCrawlCompleted;
			var start = DateTime.Now;
			var uri = new Uri("https://lord.technology");
			CrawlResult result = crawler.Crawl(uri);
			if (result.ErrorOccurred)
			{
				Console.WriteLine("Crawl of {0} completed with error: {1}", result.RootUri.AbsoluteUri, result.ErrorException.Message);
			}
			else
			{
				Console.WriteLine("Crawl of {0} completed without error.", result.RootUri.AbsoluteUri);
			}
			var finish = DateTime.Now;
			Console.WriteLine((finish - start).TotalMinutes);

			using (FileStream fs = File.Open(@"./crawl.json", FileMode.Create))
			using (StreamWriter sw = new StreamWriter(fs))
			using (JsonWriter jw = new JsonTextWriter(sw))
			{
				jw.Formatting = Formatting.Indented;
				JsonSerializer serializer = new JsonSerializer();
				serializer.Serialize(jw, new { nodes = _pages, edges = _relationships });
			}
		}

		private static void Crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
		{
			CrawledPage crawledPage = e.CrawledPage;

			if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
			{
				Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
			}
			else
			{
				if (string.IsNullOrEmpty(crawledPage.Content.Text))
				{
					Console.WriteLine("Crawl of page succeeded {0} but no content was found.", crawledPage.Uri.AbsoluteUri);
					return;
				}

				Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);

				Page parent = _pages.Add(new Page() { label = crawledPage.Uri.AbsoluteUri, lastCrawl = crawledPage.RequestCompleted });
				foreach (var uri in crawledPage.ParsedLinks)
				{
					if (crawledPage.Uri.AbsoluteUri == uri.AbsoluteUri)
					{
						continue;
					}
					var child = _pages.Add(new Page() { label = uri.AbsoluteUri });
					_relationships.Add(new Relationship() { source = parent.id, target = child.id });
				}
			}
		}
	}

	public class PageConcurrentBag : ConcurrentBag<Page>
	{
		private int _nextId = 0;
		public int NextId
		{
			get
			{
				var r = _nextId;
				_nextId++;
				return r;
			}
		}
		private int _nextCoordinate = 0;
		public int NextCoordinate
		{
			get
			{
				var r = _nextCoordinate;
				_nextCoordinate++;
				return r;
			}
		}
		public new Page Add(Page item)
		{
			var existingItem = this.Where(p => p.label == item.label).FirstOrDefault();
			if (existingItem != null)
			{
				item = existingItem;
			}
			else
			{
				item.id = NextId.ToString();
				item.x = NextCoordinate;
				item.y = NextCoordinate;
				base.Add(item);
			}
			return item;
		}
	}

	public class RelationshipConcurrentBag : ConcurrentBag<Relationship>
	{
		private int _nextId = 0;
		public int NextId
		{
			get
			{
				var r = _nextId;
				_nextId++;
				return r;
			}
		}
		public new Relationship Add(Relationship item)
		{
			var existingItem = this.Where(r => r.source == item.source && r.target == item.target).FirstOrDefault();
			if (existingItem != null)
			{
				item = existingItem;
			}
			else
			{
				item.id = NextId.ToString();
				base.Add(item);
			}
			return item;
		}
	}
}