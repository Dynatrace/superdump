using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace SuperDumpService {
	public static class Program {
		public static void Main(string[] args) {
			var host = new WebHostBuilder()
				.UseKestrel(opt => opt.Limits.MaxRequestBodySize = 8589934592L)  // 8gb
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseIISIntegration()
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
