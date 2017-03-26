using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;

namespace Spindel
{
    public class Program
    {
		private const string URLS = @"./urls.txt";
		private const string RELATIONSHIPS = @"./relationships.txt";

        public static void Main(string[] args)
        {
			File.AppendText(URLS).Dispose();
			File.AppendText(RELATIONSHIPS).Dispose();
			var root = new Page("https://www.theguardian.com/uk");
			ProcessPage(root);

			var arePagesLeft = true;
			while (arePagesLeft)
			{
				var nextParent = NextPageToCrawl();
				if (nextParent == null)
				{
					arePagesLeft = false;
					continue;
				}
				ProcessPage(nextParent);
			}
        }

		private static void ProcessPage(Page page)
		{
			page.Id = GetExistingOrNewUrlId(page.Url);
			InsertPage(page);
			page.GetChildUrls();
			foreach (var c in page.ChildUrls)
			{
				var child = new Page(c);
				child.Id = GetExistingOrNewUrlId(child.Url);
				InsertPage(child);
				InsertRelationship(page, child);
			}
		}

		private static void InsertPage(Page page)
		{
			if (GetExistingUrlId(page.Url) == null)
			{
				var line = page.Id.ToString() + "#" + page.Url;
				using (var writer = new StreamWriter(URLS, true))
				{
					writer.WriteLine(line);
				}
			}
		}

		private static void InsertRelationship(Page parent, Page child)
		{
			if (!DoesRelationshipExist(parent.Id, child.Id))
			{
				var line = parent.Id.ToString() + "#" + child.Id.ToString();
				using (var writer = new StreamWriter(RELATIONSHIPS, true))
				{
					writer.WriteLine(line);
				}
			}
		}

		private static Page DecodeLine(string line)
		{
			try
			{
				var page = new Page();
				page.Id = int.Parse(line.Split('#')[0]);
				page.Url = line.Split('#')[1];
				return page;
			}
			catch
			{
				return null;
			}
		}

		private static Page GetPage(int id)
		{
			var lines = File.ReadLines(URLS).Where(l => l.StartsWith(id.ToString() + "#", StringComparison.Ordinal));
			int count = 0;
			string line = "";
			foreach (var l in lines)
			{
				line = l;
				count++;
			}
			if (count == 1)
			{
				return DecodeLine(line);
			}
			else if (count == 0)
			{
				throw new Exception(string.Format("No line with ID {0} was found.", id.ToString()));
			}
			else
			{
				throw new Exception(string.Format("More than one line with ID {0} was found.", id.ToString()));
			}
		}

		private static Page GetPage(string url)
		{
			var lines = File.ReadLines(URLS).Where(l => l.Split('#')[1] == url);
			int count = 0;
			string line = "";
			foreach (var l in lines)
			{
				line = l;
				count++;
			}
			if (count == 1)
			{
				return DecodeLine(line);
			}
			else if (count == 0)
			{
				return null;
			}
			else
			{
				throw new Exception(string.Format("More than one matching URL {0} was found.", url));
			}
		}

		private static int GetExistingOrNewUrlId(string url)
		{
			var id = GetExistingUrlId(url);
			if (id == null)
			{
				try
				{
					string lastLine = File.ReadLines(URLS).Last();
					return int.Parse(lastLine.Split('#')[0]) + 1;
				}
				catch
				{
					return 0;
				}
			}
			else
			{
				return id.Value;
			}
		}

		private static int? GetExistingUrlId(string url)
		{
			var lines = File.ReadLines(URLS).Where(l => l.Split('#')[1] == url);
			int count = 0;
			int? id = null;
			foreach (var l in lines)
			{
				id = int.Parse(l.Split('#')[0]);
				count++;
			}
			if (count == 0)
			{
				return null;
			}
			else if (count == 1)
			{
				return id;
			}
			else
			{
				throw new Exception(string.Format("More than one matching URL {0} was found.", url));
			}
		}

		private static bool DoesRelationshipExist(int parentId, int childId)
		{
			var lines = File.ReadLines(RELATIONSHIPS).Where(l => l.Split('#')[0] == parentId.ToString() && l.Split('#')[1] == childId.ToString());
			int count = 0;
			foreach (var l in lines)
			{
				count++;
			}
			if (count == 0)
			{
				return false;
			}
			else if (count == 1)
			{
				return true;
			}
			else
			{
				throw new Exception(string.Format("More than one relationship was found between {0} and {1}.", parentId.ToString(), childId.ToString()));
			}
		}

		private static Page NextPageToCrawl()
		{
			var urls = File.ReadLines(URLS);
			foreach (var urlLine in urls)
			{
				Page parent = DecodeLine(urlLine);
				if (parent == null)
				{
					continue;
				}
				if (!File.ReadLines(RELATIONSHIPS).Any(l => l.Split('#')[0] == parent.Id.ToString()))
				{
					return parent;
				}
			}
			return null;
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

			public int Id { get; set; }

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
							// Ensure links to this page aren't regestered as child links
							if (link != Url)
							{
								links.Add(link);
							}
						}
					}
				}
				ChildUrls = links;
			}
		}
    }
}