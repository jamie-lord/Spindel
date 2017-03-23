using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using HtmlAgilityPack;
using RaptorDB;

namespace Spindel
{
    public class Program
    {
		private static RaptorDB<string> db;

        public static void Main(string[] args)
        {
			db = RaptorDB<string>.Open(@"./test", false);

			Global.SaveTimerSeconds = 5;

			var rootPage = new Page("https://www.theguardian.com/uk");
			rootPage.GetChildren();
			db.Set(rootPage.Url, SerializeToByteArray(rootPage.Children));

            db.Shutdown();
        }

        private static byte[] SerializeToByteArray(List<Page> obj)
        {
            var binFormatter = new BinaryFormatter();
            var mStream = new MemoryStream();
            binFormatter.Serialize(mStream, obj);
            return mStream.ToArray();
        }

        private static List<Page> DeserializeFromByteArray(byte[] array)
        {
            var mStream = new MemoryStream();
            var binFormatter = new BinaryFormatter();
            mStream.Write(array, 0, array.Length);
            mStream.Position = 0;
            return binFormatter.Deserialize(mStream) as List<Page>;
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
        public List<Page> Children { get; set; }
		public void GetChildren()
		{
			if (Url != null)
			{
				if (Children == null)
				{
					Children = new List<Page>();
				}
				foreach (var link in GetHtml(Url))
				{
					Children.Add(new Page(link));
				}
			}
		}

		private static List<string> GetHtml(string url)
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
						links.Add(link);
				}
			}
			return links;
		}
    }
    #endregion
}