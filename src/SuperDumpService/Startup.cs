using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Logging;
using Hangfire.Logging.LogProviders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Sakura.AspNetCore.Mvc;
using SuperDump.Webterm;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Services;
using Swashbuckle.Swagger.Model;
using WebSocketManager;

namespace SuperDumpService {
	public class Startup {
		public Startup(IHostingEnvironment env) {
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile(Path.Combine(PathHelper.GetConfDirectory(), "appsettings.json"), optional: false, reloadOnChange: true)
				.AddJsonFile(Path.Combine(PathHelper.GetConfDirectory(), $"appsettings.{env.EnvironmentName}.json"), optional: true)
				.AddEnvironmentVariables();

			if (env.IsDevelopment()) {
				builder.AddUserSecrets<Startup>();
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

			var superDumpSettings = new SuperDumpSettings();
			Configuration.GetSection(nameof(SuperDumpSettings)).Bind(superDumpSettings);

			// add ldap authentication
			if (superDumpSettings.UseLdapAuthentication) {
				services.AddLdapCookieAuthentication(superDumpSettings.LdapAuthenticationSettings, new LdapAuthenticationPathOptions {
					LoginPath = "/Login/Index/",
					LogoutPath = "/Login/Logout/",
					AccessDeniedPath = "/Login/AccessDenied/"
				});
				services.AddSingleton(typeof(IAuthorizationHelper), typeof(AuthorizationHelper));
			} else {
				services.AddPoliciesForNoAuthentication();
				services.AddSingleton(typeof(IAuthorizationHelper), typeof(NoAuthorizationHelper));
			}

			services.AddAntiforgery();

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
			services.AddSingleton<DumpRetentionService>();
			services.AddSingleton<SimilarityService>();
			services.AddSingleton<RelationshipRepository>();
			services.AddSingleton<RelationshipStorageFilebased>();
			services.AddSingleton<IdenticalDumpStorageFilebased>();
			services.AddSingleton<IdenticalDumpRepository>();
			services.AddSingleton<JiraApiService>();
			services.AddSingleton<JiraIssueStorageFilebased>();
			services.AddSingleton<JiraIssueRepository>();
			services.AddWebSocketManager();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<SuperDumpSettings> settings, IServiceProvider serviceProvider, SlackNotificationService sns, IAuthorizationHelper authorizationHelper) {
			app.ApplicationServices.GetService<BundleRepository>().Populate();
			app.ApplicationServices.GetService<DumpRepository>().Populate();
			Task.Run(async () => await app.ApplicationServices.GetService<RelationshipRepository>().Populate());
			Task.Run(async () => await app.ApplicationServices.GetService<IdenticalDumpRepository>().Populate());
			if (settings.Value.UseJiraIntegration) {
				Task.Run(() => app.ApplicationServices.GetService<JiraIssueRepository>().Populate());
			}

			// configure Logger
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));

			var fileLogConfig = Configuration.GetSection("FileLogging");
			var logPath = Path.GetDirectoryName(fileLogConfig.GetValue<string>("PathFormat"));
			Directory.CreateDirectory(logPath);
			loggerFactory.AddFile(Configuration.GetSection("FileLogging"));
			loggerFactory.AddFile(Configuration.GetSection("RequestFileLogging"));
			loggerFactory.AddDebug();


			if (settings.Value.UseHttpsRedirection) {
				app.UseHttpsRedirection();
			}
			if (settings.Value.UseLdapAuthentication) {
				app.UseAuthentication();
				app.UseSwaggerAuthorizationMiddleware(authorizationHelper);
			} else {
				app.MapWhen(context => context.Request.Path.StartsWithSegments("/Login") || context.Request.Path.StartsWithSegments("/api/Token"),
					appBuilder => appBuilder.Run(async context => {
						context.Response.StatusCode = 404;
						await context.Response.WriteAsync("");
					}));
			}

			ILogger logger = loggerFactory.CreateLogger("SuperDumpServiceRequests");
			app.Use(async (context, next) => {
				logger.LogRequest(context);
				await next.Invoke();
			});

			app.UseHangfireDashboard("/hangfire", new DashboardOptions {
				Authorization = new[] { new CustomAuthorizeFilter(authorizationHelper) }
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
			app.UseHangfireServer(new BackgroundJobServerOptions {
				Queues = new[] { "retention" },
				WorkerCount = 1
			});
			app.UseHangfireServer(new BackgroundJobServerOptions {
				Queues = new[] { "similarityanalysis" },
				WorkerCount = 8
			});
			if (settings.Value.UseJiraIntegration) {
				app.UseHangfireServer(new BackgroundJobServerOptions {
					Queues = new[] { "jirastatus" },
					WorkerCount = 2
				});

				JiraIssueRepository jiraIssueRepository = app.ApplicationServices.GetService<JiraIssueRepository>();
				jiraIssueRepository.StartRefreshHangfireJob();
				jiraIssueRepository.StartBundleSearchHangfireJob();
			}
			app.ApplicationServices.GetService<DumpRetentionService>().StartService();

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

			app.UseMvcWithDefaultRoute();
		}
	}

	public class CustomAuthorizeFilter : IDashboardAuthorizationFilter {
		private IAuthorizationHelper authorizationHelper;

		public CustomAuthorizeFilter(IAuthorizationHelper authorizationHelper) {
			this.authorizationHelper = authorizationHelper;
		}

		public bool Authorize([NotNull] DashboardContext context) {
			return authorizationHelper.CheckPolicy(context.GetHttpContext().User, LdapCookieAuthenticationExtension.AdminPolicy);
		}
	}
}