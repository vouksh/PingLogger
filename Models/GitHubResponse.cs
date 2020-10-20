using System;
using System.Collections.Generic;
using System.Text;

namespace PingLogger.GUI.Models
{
	public class GitHubResponse
	{
		public string url { get; set; }
		public string name { get; set; }
		public string tag_name { get; set; }
		public DateTime published_at { get; set; }
		public List<Asset> assets { get; set; }
	}
	public class Asset
	{
		public string url { get; set; }
		public string name { get; set; }
		public string browser_download_url { get; set; }
	}
}
