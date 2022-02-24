using System.Text.Json;
using IPFetch.Configuration;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;

public class IPFetchService
{
	private readonly ILogger<IPFetchService> _logger;
	private readonly IOptions<IPFetchConfig> _config;
	private static HttpClient _httpClient = new HttpClient();

	public IPFetchService(IOptions<IPFetchConfig> config, ILogger<IPFetchService> logger) =>
		(_config, _logger) = (config, logger);

	public async Task CheckIP()
	{
		var currentIP = await GetCurrentIP();
		var cache = GetCacheState();

		if (currentIP != cache.CachedIP && currentIP != null)
		{
			_logger.LogInformation("IP has changed!");

			cache.CachedIP = currentIP;
			cache.NotificationSentSinceLastChange = false;
			SaveCacheState(cache);
			await UpdateDNS();
		}

		if (!cache.NotificationSentSinceLastChange)
		{
			await SendEmail(cache.CachedIP);
		}
	}

	private async Task<string?> GetCurrentIP()
	{
		try
		{
			var currentIP = await (await _httpClient.GetAsync(_config.Value.IpAddressProviderUrl))
				.Content
				.ReadAsStringAsync();

			return currentIP.Trim();
		}
		catch (Exception ex)
		{
			_logger.LogError("Unable to get IP address from provider", ex);
			return null;
		}
	}

	private async Task SendEmail(string? ipAddress)
	{
		if (ipAddress == null)
		{
			_logger.LogWarning("Attempted to send notifications on IP address with a null value.");
			return;
		}

		if (
			string.IsNullOrWhiteSpace(_config.Value.MailgunAPIKey) ||
			string.IsNullOrWhiteSpace(_config.Value.MailgunDomainName)
		)
		{
			_logger.LogWarning("Mailgun configuration incomplete. Unable to send e-mail notification.");
			return;
		}

		_logger.LogInformation("Sending notification email");

		try
		{
			RestClient client = new RestClient("https://api.mailgun.net/v3");
			client.Authenticator = new HttpBasicAuthenticator("api", $"{_config.Value.MailgunAPIKey}");

			RestRequest request = new RestRequest();
			request.AddParameter("domain", _config.Value.MailgunDomainName, ParameterType.UrlSegment);
			request.Resource = "{domain}/messages";
			request.AddParameter("from", $"IPFetch <ipfetch-noreply@{_config.Value.MailgunDomainName}>");
			request.AddParameter("to", $"{_config.Value.ReceiverName} <{_config.Value.ReceiverEmail}>");
			request.AddParameter("subject", $"IPFetch update for {Environment.MachineName}");
			request.AddParameter("text",
				$"Hi {_config.Value.ReceiverName},\r\n\r\nThe IP for your machine ({Environment.MachineName}) has changed since we last checked and is now: {ipAddress}");
			request.Method = Method.Post;

			await client.ExecuteAsync(request);

			var cache = GetCacheState();
			cache.NotificationSentSinceLastChange = true;
			SaveCacheState(cache);
		}
		catch (Exception ex)
		{
			_logger.LogError("Failed to send e-mail notification.", ex);
		}
	}

	private async Task UpdateDNS()
	{
		var dnsUpdateUrls = _config.Value.DnsUpdateUrls?.Split(",") ?? new string[0];

		_logger.LogInformation("Updating DNS");

		foreach (var url in dnsUpdateUrls)
		{
			if (!string.IsNullOrWhiteSpace(url))
			{
				try
				{
					var response = await (await _httpClient.GetAsync(url)).Content.ReadAsStringAsync();
					_logger.LogInformation(response);
				}
				catch (Exception ex)
				{
					_logger.LogError($"Failed to update DNS ({url})", ex);
				}
			}
		}

		_logger.LogInformation("Finished updating DNS");

	}

	private IPFileCache GetCacheState()
	{
		var filePath = _config.Value.CacheFilePath;
		if (filePath == null) throw new Exception("Cache file path is null. Please set it up in config.");
		if (File.Exists(filePath))
		{
			var contents = File.ReadAllText(filePath);
			return JsonSerializer.Deserialize<IPFileCache>(contents) ?? CreateNewFileCache();
		}
		else
		{
			return CreateNewFileCache();
		}
	}

	private IPFileCache CreateNewFileCache()
	{
		return new IPFileCache
		{
			NotificationSentSinceLastChange = true,
			CachedIP = null,
		};
	}

	private void SaveCacheState(IPFileCache cache)
	{
		var filePath = _config.Value.CacheFilePath;
		if (filePath == null) throw new Exception("Cache file path is null. Please set it up in config.");
		if (!Directory.Exists(Path.GetDirectoryName(filePath))) throw new Exception($"Cache file path directory does not exist. Please create it ({Path.GetDirectoryName(filePath)}).");
		var serialized = JsonSerializer.Serialize(cache);
		File.WriteAllText(filePath, serialized);
	}
}
