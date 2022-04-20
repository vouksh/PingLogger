using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PingLogger.Extensions;

public class DownloadClient
{
	public delegate void FileDownloadedEventHandler(object sender, bool completed);

	public event FileDownloadedEventHandler FileDownloaded;
	private readonly HttpClient _httpClient;
	private readonly string _remoteURL;
	private readonly string _localFile;

	public DownloadClient(string remoteUrl, string localFileName)
	{
		_remoteURL = remoteUrl;
		_localFile = localFileName;
		_httpClient = new HttpClient();
	}
	
	public async Task DownloadFileTaskAsync()
	{
		await using (var s = await _httpClient.GetStreamAsync(_remoteURL))
		{
			await using (var fs = new FileStream(_localFile, FileMode.CreateNew))
			{
				await s.CopyToAsync(fs);
			}
		}
		FileDownloaded?.Invoke(this, true);
	}
}