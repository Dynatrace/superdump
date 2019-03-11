using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SuperDumpService.Helpers;
using Microsoft.Extensions.Hosting;
using Serilog.Extensions.Logging;

namespace SuperDumpService {
	public static class Program {

		public static void Main(string[] args) {
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseKestrel(opt => opt.Limits.MaxRequestBodySize = long.MaxValue);
					webBuilder.ConfigureAppConfiguration((hostingContext, config) => {
						config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
						config.AddJsonFile(Path.Combine(PathHelper.GetConfDirectory(), "appsettings.json"), optional: false, reloadOnChange: true);
						config.AddJsonFile(Path.Combine(PathHelper.GetConfDirectory(), $"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json"), optional: true);
						config.AddEnvironmentVariables();
						if (hostingContext.HostingEnvironment.IsDevelopment()) {
							config.AddUserSecrets<Startup>();
						}
					});
					webBuilder.ConfigureLogging((hostingContext, logging) => {
						logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
						
						var fileLogConfig = hostingContext.Configuration.GetSection("FileLogging");
						var logPath = Path.GetDirectoryName(fileLogConfig.GetValue<string>("PathFormat"));
						Directory.CreateDirectory(logPath);
						logging.AddFile(hostingContext.Configuration.GetSection("FileLogging"));
						logging.AddFile(hostingContext.Configuration.GetSection("RequestFileLogging"));
					});
					webBuilder.UseStartup<Startup>();
				});
	}
}

