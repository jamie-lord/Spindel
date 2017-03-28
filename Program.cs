using HtmlAgilityPack;
using Spindel.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using System.Net;

namespace Spindel
{
    public class Program
    {
        private static readonly SpindelContext db = new SpindelContext();
        public static void Main(string[] args)
        {
            var root = new Page() { Url = "https://www.theguardian.com/uk" };
            CrawlPage(root);
            Page nextPage;
            do
            {
                nextPage = NextPageToCrawl();
                if (nextPage != null)
                {
                    CrawlPage(nextPage);
                }
            } while (nextPage != null);
        }

        private static void CrawlPage(Page parent)
        {
            parent = db.Pages.AddIfNotExists(parent, p => p.Url == parent.Url);
            var chidren = GetChildUrls(parent.Url);
            foreach (var url in chidren)
            {
                Page child = new Page() { Url = url };
                child = db.Pages.AddIfNotExists(child, p => p.Url == child.Url);
                db.Relationships.AddOrUpdate(new Relationship() { Parent = parent, Child = child });
            }
            parent.LastCrawl = DateTime.Now;
            db.Pages.AddOrUpdate(parent);
            db.SaveChanges();
        }

        private static Page NextPageToCrawl()
        {
            return db.Pages.Where(p => p.LastCrawl == null).First();
        }

        public static List<string> GetChildUrls(string url)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.OptionFixNestedTags = true;
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:31.0) Gecko/20100101 Firefox/31.0";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-us,en;q=0.5");
                WebResponse response = request.GetResponse();
                htmlDoc.Load(response.GetResponseStream(), true);
                var links = new List<string>();
                foreach (HtmlNode hrefs in htmlDoc.DocumentNode.SelectNodes("//a[@href]"))
                {
                    HtmlAttribute att = hrefs.Attributes["href"];
                    foreach (var link in att.Value.Split(' '))
                    {
                        if (link.StartsWith("http", StringComparison.Ordinal) && !links.Contains(link))
                        {
                            // Ensure links to this page aren't registered as child links
                            if (link != url)
                            {
                                links.Add(link);
                            }
                        }
                    }
                }
                Console.WriteLine(string.Format("Found {0} child URLs for URL:{1}", links.Count(), url));
                return links;
            }
            catch
            {
                Console.WriteLine(string.Format("Error getting child links for URL:{0}", url));
                return null;
            }
        }
    }

    public static class DbSetExtensions
    {
        public static T AddIfNotExists<T>(this DbSet<T> dbSet, T entity, Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            var exists = predicate != null ? dbSet.Any(predicate) : dbSet.Any();
            return !exists ? dbSet.Add(entity) : dbSet.Where(predicate).First();
        }
    }

    public class SpindelContext : DbContext
    {
        public DbSet<Page> Pages { get; set; }
        public DbSet<Relationship> Relationships { get; set; }
    }
}