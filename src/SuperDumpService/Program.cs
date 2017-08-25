using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace SuperDumpService {
	public static class Program {
		public static void Main(string[] args) {
			var host = new WebHostBuilder()
				.UseKestrel(opt => opt.Limits.MaxRequestBodySize = long.MaxValue)
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseIISIntegration()
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
