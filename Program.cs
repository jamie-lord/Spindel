using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;

namespace Spindel
{
    public class Program
    {
		private const string URLS = @"./urls";
		private const string RELATIONSHIPS = @"./relationships";

        public static void Main(string[] args)
        {
			var parent = new Page("https://www.theguardian.com/uk");
			BinaryRage.DB.Insert(parent.Key, parent.Url, URLS);
			parent.GetChildUrls();
			foreach (var c in parent.ChildUrls)
			{
				var child = new Page(c);
				BinaryRage.DB.Insert(child.Key, child.Url, URLS);
				BinaryRage.DB.Insert(parent.Key, child.Key, RELATIONSHIPS);
			}
        }

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

			private string _key;
			public string Key
			{
				get
				{
					if (_key == null)
					{
						_key = BinaryRage.Key.GenerateUniqueKey();
					}
					return _key;
				}

				set
				{
						_key = value;
				}
			}

			private List<string> _childUrls;
			public List<string> ChildUrls
			{
				get
				{
					if (_childUrls == null)
					{
						_childUrls = new List<string>();
					}
					return _childUrls;
				}
				private set
				{
					_childUrls = value;
				}
			}

			public void GetChildUrls()
			{
				if (Url == null)
				{
					return;
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
				ChildUrls = links;
			}
		}
    }
}