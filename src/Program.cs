using IPFetch.Configuration;

IHost host = Host.CreateDefaultBuilder(args)
	.UseWindowsService(opt => opt.ServiceName = "IPFetch")
	.ConfigureAppConfiguration(opt =>
	{
		opt.AddJsonFile("appsettings.local.json", true);
	})
	.ConfigureServices((context, services) =>
	{
		services.AddHostedService<WindowsBackgroundService>();
		services.Configure<IPFetchConfig>(context.Configuration);
		services.AddTransient<IPFetchService>();
	})
	.Build();

await host.RunAsync();
