using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SuperDumpService.Helpers;

namespace SuperDumpService {
	public static class Program {
		public static void Main(string[] args) {
			CreateWebHostBuilder(args).Build().Run();
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseKestrel(opt => opt.Limits.MaxRequestBodySize = long.MaxValue)
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseIISIntegration()
				.ConfigureAppConfiguration((hostingContext, config) => {
					config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
					config.AddJsonFile(Path.Combine(PathHelper.GetConfDirectory(), "appsettings.json"), optional: false, reloadOnChange: true);
					config.AddJsonFile(Path.Combine(PathHelper.GetConfDirectory(), $"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json"), optional: true);
					config.AddEnvironmentVariables();
					if (hostingContext.HostingEnvironment.IsDevelopment()) {
						config.AddUserSecrets<Startup>();
					}
				})
				.ConfigureLogging((hostingContext, logging) => {
					logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
				})
				.UseStartup<Startup>();
	}
}

