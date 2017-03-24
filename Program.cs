using HtmlAgilityPack;
using RaptorDB;
using System;
using System.Collections.Generic;
using System.Net;

namespace Spindel
{
    public class Program
    {
        private static RaptorDB<Guid> urlDb;
        private static RaptorDB<Guid> relationshipDb;

        public static void Main(string[] args)
        {
            urlDb = RaptorDB<Guid>.Open(@"./urlDb", false);
            relationshipDb = RaptorDB<Guid>.Open(@"./relationshipDb", true);

            Global.SaveTimerSeconds = 5;

            var rootPage = new Page("https://www.theguardian.com/uk");
            //rootPage.GetChildren();
            //urlDb.Set(rootPage.Url, SerializeToByteArray(rootPage.Children));

            urlDb.Shutdown();
        }

        private static Guid GetUnusedGuid()
        {
            Guid? result = null;
            string n;
            while (result == null)
            {
                var t = Guid.NewGuid();
                if (urlDb.Get(t, out n))
                {
                    result = t;
                }
            }
            return result.Value;
        }
    }

    #region [entities]

    [Serializable]
    public class Page
    {
        public Page()
        {
        }

        public Page(string url)
        {
            Url = url;
        }

        public string Url { get; set; }

        private List<string> GetLinks()
        {
            if (Url == null)
            {
                return null;
            }
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.OptionFixNestedTags = true;
            HttpWebRequest request = WebRequest.Create(Url) as HttpWebRequest;
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
                        links.Add(link);
                    }
                }
            }
            return links;
        }
    }

    #endregion
}