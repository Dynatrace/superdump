using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace SuperDumpService {
	public static class Program {
		public static void Main(string[] args) {
			var host = new WebHostBuilder()
				.UseKestrel(opt => opt.Limits.MaxRequestBodySize = 2147483648)  // 2gb
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseIISIntegration()
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
