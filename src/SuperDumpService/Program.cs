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
			var host = new WebHostBuilder()
				.UseKestrel(opt => opt.Limits.MaxRequestBodySize = 8589934592L)  // 8gb
				.ConfigureServices(s => s.AddSingleton<IConfigurationRoot>(Configuration))
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseIISIntegration()
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
