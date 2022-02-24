namespace IPFetch.Configuration;

public class IPFetchConfig
{
	public string? IpAddressProviderUrl { get; set; }
	public string? ReceiverEmail { get; set; }
	public string? ReceiverName { get; set; }
	public string? CacheFilePath { get; set; }
	public int IpCheckIntervalSeconds { get; set; }
	public string? MailgunDomainName { get; set; }
	public string? MailgunAPIKey { get; set; }
	public string? DnsUpdateUrls { get; set; }
}
