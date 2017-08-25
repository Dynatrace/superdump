using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Hangfire;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.Swagger.Model;
using Hangfire.Logging;
using Hangfire.Logging.LogProviders;
using SuperDumpService.Helpers;
using Hangfire.Dashboard;
using Hangfire.Annotations;
using Microsoft.AspNetCore.Http.Features;
using System.IO;
using Microsoft.Extensions.Options;
using SuperDumpService.Services;
using System.Linq;
using SuperDump.Webterm;
using WebSocketManager;
using Sakura.AspNetCore.Mvc;

namespace SuperDumpService {
	public class Startup {
		public Startup(IHostingEnvironment env) {
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile(Path.Combine(PathHelper.GetConfDirectory(), "appsettings.json"), optional: false, reloadOnChange: true)
				.AddJsonFile(Path.Combine(PathHelper.GetConfDirectory(), $"appsettings.{env.EnvironmentName}.json"), optional: true)
				.AddEnvironmentVariables();

			if (env.IsDevelopment()) {
				//builder.AddApplicationInsightsSettings(developerMode: true);
			}

			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) {
			// setup path
			IConfigurationSection configurationSection = Configuration.GetSection(nameof(SuperDumpSettings));
			IConfigurationSection binPathSection = configurationSection.GetSection(nameof(SuperDumpSettings.BinPath));
			IEnumerable<string> binPath = binPathSection.GetChildren().Select(s => s.Value);
			string path = Environment.GetEnvironmentVariable("PATH");
			string additionalPath = string.Join(";", binPath);
			Environment.SetEnvironmentVariable("PATH", path + ";" + additionalPath);

			services.AddOptions();
			services.Configure<SuperDumpSettings>(Configuration.GetSection(nameof(SuperDumpSettings)));

			var pathHelper = new PathHelper(Configuration.GetSection(nameof(SuperDumpSettings)));
			services.AddSingleton(pathHelper);

			//configure DB
			if (Configuration.GetValue<bool>("UseInMemoryHangfireStorage")) {
				services.AddHangfire(x => x.UseStorage(new Hangfire.MemoryStorage.MemoryStorage()));
			} else {
				string connString;
				Console.WriteLine(Directory.GetCurrentDirectory());
				using (SqlConnection conn = LocalDBAccess.GetLocalDB(Configuration, "HangfireDB", pathHelper)) {
					connString = conn.ConnectionString;
				}
				if (string.IsNullOrEmpty(connString)) {
					throw new Exception("DB could not be created, please check if LocalDB is installed.");
				}
				services.AddHangfire(x => x.UseSqlServerStorage(connString));
			}

			// set upload limit
			int maxUploadSizeMB = Configuration.GetSection(nameof(SuperDumpSettings)).GetValue<int>(nameof(SuperDumpSettings.MaxUploadSizeMB));
			if (maxUploadSizeMB == 0) maxUploadSizeMB = 16000; // default
			services.Configure<FormOptions>(opt => opt.MultipartBodyLengthLimit = 1024L * 1024L * maxUploadSizeMB);

			// Add framework services.
			services.AddMvc();
			//services.AddCors();
			services.AddSwaggerGen();

			services.ConfigureSwaggerGen(options => {
				options.SingleApiVersion(new Info {
					Version = "v1",
					Title = "SuperDump API",
					Description = "REST interface for SuperDump analysis tool",
					TermsOfService = "None",
					Contact = new Contact { Url = "https://github.com/Dynatrace/superdump" }
				});

				//Determine base path for the application.
				var basePath = PlatformServices.Default.Application.ApplicationBasePath;

				//Set the comments path for the swagger json and ui.
				var xmlDocFile = new FileInfo(Path.Combine(basePath, "SuperDumpService.xml"));
				if (xmlDocFile.Exists) {
					options.IncludeXmlComments(xmlDocFile.FullName);
				}
			});

			// for pagination list
			services.AddBootstrapPagerGenerator(options => {
				options.ConfigureDefault();
			});

			// App Insights
			services.AddApplicationInsightsTelemetry(Configuration);

			// register repository as singleton
			services.AddSingleton<SuperDumpRepository>();

			services.AddSingleton<BundleRepository>();
			services.AddSingleton<BundleStorageFilebased>();
			services.AddSingleton<DumpRepository>();
			services.AddSingleton<DumpStorageFilebased>();
			services.AddSingleton<AnalysisService>();
			services.AddSingleton<DownloadService>();
			services.AddSingleton<SymStoreService>();
			services.AddSingleton<UnpackService>();
			services.AddSingleton<NotificationService>();
			services.AddSingleton<SlackNotificationService>();
			services.AddSingleton<ElasticSearchService>();
			services.AddWebSocketManager();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<SuperDumpSettings> settings, IServiceProvider serviceProvider, SlackNotificationService sns) {
			app.ApplicationServices.GetService<BundleRepository>().Populate();
			app.ApplicationServices.GetService<DumpRepository>().Populate();

			//foreach(var b in app.ApplicationServices.GetService<BundleRepository>().GetAll()) {
			//	foreach(var d in app.ApplicationServices.GetService<DumpRepository>().Get(b.BundleId)) {
			//		var msg = sns.GetMessage2(d);
			//		Console.WriteLine(msg);
			//	}
			//}

			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			app.UseHangfireDashboard("/hangfire", new DashboardOptions {
				Authorization = new[] { new CustomAuthorizeFilter() }
			});

			app.UseHangfireServer(new BackgroundJobServerOptions {
				Queues = new[] { "download" },
				WorkerCount = settings.Value.MaxConcurrentBundleExtraction
			});
			app.UseHangfireServer(new BackgroundJobServerOptions {
				Queues = new[] { "analysis" },
				WorkerCount = settings.Value.MaxConcurrentAnalysis
			});
			app.UseHangfireServer(new BackgroundJobServerOptions {
				Queues = new[] { "elasticsearch" },
				WorkerCount = 1
			});

			GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });

			app.UseSwagger();
			app.UseSwaggerUi();

			LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider());

			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				BrowserLinkExtensions.UseBrowserLink(app); // using the extension method directly somehow did not work in .NET Core 2.0 (ambiguous extension method)
			} else {
				app.UseExceptionHandler("/Home/Error");
			}

			app.UseStaticFiles();

			app.UseWebSockets();
			app.MapWebSocketManager("/cmd", serviceProvider.GetService<WebTermHandler>());

			app.UseMvc(routes => {
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}

	public class CustomAuthorizeFilter : IDashboardAuthorizationFilter {
		public bool Authorize([NotNull] DashboardContext context) {
			return true; // let everyone see hangfire
		}
	}
}
