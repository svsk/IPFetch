using IPFetch.Configuration;
using Microsoft.Extensions.Options;

public sealed class WindowsBackgroundService : BackgroundService
{
	private readonly IPFetchService _ipFetchService;
	private readonly ILogger<WindowsBackgroundService> _logger;
	private readonly IOptions<IPFetchConfig> _config;
	private readonly IPFetchService _service;

	public WindowsBackgroundService(
		IPFetchService ipFetchService,
		ILogger<WindowsBackgroundService> logger,
		IOptions<IPFetchConfig> config,
		IPFetchService service
	) => (_ipFetchService, _logger, _config, _service) = (ipFetchService, logger, config, service);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("IPFetch is starting...");

		while (!stoppingToken.IsCancellationRequested)
		{
			await _service.CheckIP();
			await Task.Delay(TimeSpan.FromSeconds(_config.Value.IpCheckIntervalSeconds), stoppingToken);
		}

		_logger.LogInformation("IPFetch is shutting down.");
	}
}
