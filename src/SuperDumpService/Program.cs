using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SuperDumpService.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace SuperDumpService {
	public static class Program {
		public static IConfigurationRoot Configuration { get; set;  }

		public static void Main(string[] args) {
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile(Path.Combine(PathHelper.GetConfDirectory(), "appsettings.json"), optional: false, reloadOnChange: true)
				.AddEnvironmentVariables();
			Configuration = builder.Build();
			int maxUploadSizeMB = Configuration.GetSection(nameof(SuperDumpSettings)).GetValue<int>(nameof(SuperDumpSettings.MaxUploadSizeMB));
			if(maxUploadSizeMB == 0) {
				maxUploadSizeMB = 16000;
			}

			var host = new WebHostBuilder()
				.UseKestrel(opt => opt.Limits.MaxRequestBodySize = 1024L * 1024L * maxUploadSizeMB)
				.ConfigureServices(s => s.AddSingleton<IConfigurationRoot>(Configuration))
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseIISIntegration()
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
