using IPFetch.Configuration;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
	.UseWindowsService(opt => opt.ServiceName = "IPFetch")
	.ConfigureAppConfiguration(opt =>
	{
		opt.AddJsonFile("appsettings.local.json", true);
	})
	.ConfigureServices((context, services) =>
	{
		var logConfig = new LoggerConfiguration()
			.Enrich.FromLogContext();

		var shouldLogToFile = context.Configuration["enableLoggingToFile"] == "True";
		if (shouldLogToFile)
		{
			var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs/IPFetchLog.txt");

			logConfig.WriteTo.File(
				path,
				rollingInterval: RollingInterval.Day,
				fileSizeLimitBytes: 10500000
			);
		}

		Log.Logger = logConfig.CreateLogger();

		services.AddLogging(builder => builder.AddSerilog(dispose: true));
		services.AddHostedService<WindowsBackgroundService>();
		services.Configure<IPFetchConfig>(context.Configuration);
		services.AddTransient<IPFetchService>();
	})
	.Build();

await host.RunAsync();
