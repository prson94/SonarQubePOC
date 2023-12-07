using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace igx.jobs
{
	public static class HostBuilderExtensions
	{
		public static IHostBuilder SetGovernConfiguration(this IHostBuilder host)
		{
			return host.ConfigureAppConfiguration((context, b) =>
			{
				b.AddConfiguration(context.Configuration).Build();
			});
		}

		public static IHostBuilder ConfigureGovernLogging(this IHostBuilder host)
		{
			return host.ConfigureLogging((context, b) =>
			{

				var logLevel = LogLevel.Warning;
				var configLogLevel = context.Configuration["LogLevel"];
				if (!string.IsNullOrEmpty(configLogLevel))
				{
					Enum.TryParse(configLogLevel, out logLevel);
				}
				b.SetMinimumLevel(logLevel);
#if DEBUG
				b.AddConsole();
#endif
				b.AddApplicationInsights(c =>
				{
					c.ConnectionString = context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
				}, o =>
				{
					o.FlushOnDispose = true;
				});
			})
			.UseConsoleLifetime();
		}
	}
}
