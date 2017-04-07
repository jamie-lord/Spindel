using Abot.Crawler;
using Abot.Poco;
using NanoApi;
using Spindel.Models;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

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
            var startString = DateTime.UtcNow.ToString("o");
            foreach (var c in Path.GetInvalidFileNameChars()) { startString = startString.Replace(c, '-'); }
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

            var pagesDb = JsonFile<Page>.GetInstance(".\\", string.Format("{0}_pages.json", startString), Encoding.UTF8);
            pagesDb.Insert(_pages.ToList());
            var relationshipsDb = JsonFile<Relationship>.GetInstance(".\\", string.Format("{0}_relationships.json", startString), Encoding.UTF8);
            relationshipsDb.Insert(_relationships.ToList());
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

                Page parent = _pages.Add(new Page() { Uri = crawledPage.Uri.AbsoluteUri, LastCrawl = crawledPage.RequestCompleted });
                foreach (var uri in crawledPage.ParsedLinks)
                {
                    if (crawledPage.Uri.AbsoluteUri == uri.AbsoluteUri)
                    {
                        continue;
                    }
                    var child = _pages.Add(new Page() { Uri = uri.AbsoluteUri });
                    _relationships.Add(new Relationship() { Parent = parent.Id, Child = child.Id });
                }
            }
        }
    }

    public class PageConcurrentBag : ConcurrentBag<Page>
    {
        private int _nextId = 1;
        public int NextId
        {
            get
            {
                var r = _nextId;
                _nextId++;
                return r;
            }
        }
        public new Page Add(Page item)
        {
            var existingItem = this.Where(p => p.Uri == item.Uri).FirstOrDefault();
            if (existingItem != null)
            {
                item = existingItem;
            }
            else
            {
                item.Id = NextId;
                base.Add(item);
            }
            return item;
        }
    }

    public class RelationshipConcurrentBag : ConcurrentBag<Relationship>
    {
        private int _nextId = 1;
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
            var existingItem = this.Where(r => r.Parent == item.Parent && r.Child == item.Child).FirstOrDefault();
            if (existingItem != null)
            {
                item = existingItem;
            }
            else
            {
                item.Id = NextId;
                base.Add(item);
            }
            return item;
        }
    }
}