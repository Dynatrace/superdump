using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dynatrace.OneAgent.Sdk.Api;
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
using SuperDump.Webterm;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Services;
using SuperDumpService.Services.Analyzers;
using WebSocketManager;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.OpenApi.Models;
using Polly;

namespace SuperDumpService {
	public class Startup {
		private readonly IWebHostEnvironment env;
		private readonly IConfiguration config;

		public Startup(IWebHostEnvironment env, IConfiguration config) {
			this.env = env;
			this.config = config;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) {
			// setup path
			IConfigurationSection configurationSection = config.GetSection(nameof(SuperDumpSettings));
			IConfigurationSection binPathSection = configurationSection.GetSection(nameof(SuperDumpSettings.BinPath));
			IEnumerable<string> binPath = binPathSection.GetChildren().Select(s => s.Value);
			string path = Environment.GetEnvironmentVariable("PATH");
			string additionalPath = string.Join(";", binPath);
			Environment.SetEnvironmentVariable("PATH", path + ";" + additionalPath);

			services.AddOptions();
			services.Configure<SuperDumpSettings>(config.GetSection(nameof(SuperDumpSettings)));

			var pathHelper = new PathHelper(
				configurationSection.GetValue<string>(nameof(SuperDumpSettings.DumpsDir)) ?? Path.Combine(Directory.GetCurrentDirectory(), @"../../data/dumps/"),
				configurationSection.GetValue<string>(nameof(SuperDumpSettings.UploadDir)) ?? Path.Combine(Directory.GetCurrentDirectory(), @"../../data/uploads/"),
				configurationSection.GetValue<string>(nameof(SuperDumpSettings.HangfireLocalDbDir)) ?? Path.Combine(Directory.GetCurrentDirectory(), @"../../data/hangfire/")
			);
			services.AddSingleton(pathHelper);

			var superDumpSettings = new SuperDumpSettings();
			config.GetSection(nameof(SuperDumpSettings)).Bind(superDumpSettings);

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

			// asp.net core health checks
			services.AddHealthChecks();

			//configure DB
			if (config.GetValue<bool>("UseInMemoryHangfireStorage")) {
				services.AddHangfire(x => x.UseStorage(new Hangfire.MemoryStorage.MemoryStorage()));
			} else {
				string connString;
				Console.WriteLine(Directory.GetCurrentDirectory());
				using (SqlConnection conn = LocalDBAccess.GetLocalDB(config, "HangfireDB", pathHelper)) {
					connString = conn.ConnectionString;
				}
				if (string.IsNullOrEmpty(connString)) {
					throw new Exception("DB could not be created, please check if LocalDB is installed.");
				}
				services.AddHangfire(x => x.UseSqlServerStorage(connString));
			}

			// set upload limit
			int maxUploadSizeMB = config.GetSection(nameof(SuperDumpSettings)).GetValue<int>(nameof(SuperDumpSettings.MaxUploadSizeMB));
			if (maxUploadSizeMB == 0) maxUploadSizeMB = 16000; // default
			services.Configure<FormOptions>(opt => opt.MultipartBodyLengthLimit = 1024L * 1024L * maxUploadSizeMB);

			// Add framework services.
			services.AddMvc()
				.AddNewtonsoftJson();
			services.AddSwaggerGen();

			services.AddSwaggerGen(options => {
				options.SwaggerDoc("v1", new OpenApiInfo {
					Version = "v1",
					Title = "SuperDump API",
					Description = "REST interface for SuperDump analysis tool",
					Contact = new OpenApiContact { Url = new Uri("https://github.com/Dynatrace/superdump") }
				});

				//Determine base path for the application.
				var basePath = PlatformServices.Default.Application.ApplicationBasePath;

				//Set the comments path for the swagger json and ui.
				var xmlDocFile = new FileInfo(Path.Combine(basePath, "SuperDumpService.xml"));
				if (xmlDocFile.Exists) {
					options.IncludeXmlComments(xmlDocFile.FullName);
				}


				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme() {
					In = ParameterLocation.Header,
					Description = "Please insert JWT Bearer Token into field",
					Name = "Authorization",
					Type = SecuritySchemeType.ApiKey
				});
			});

			// Add HttpErrorPolicy for ObjectDisposedException when downloading a dump file using the DownloadService
			services.AddHttpClient(DownloadService.HttpClientName, config => config.Timeout = superDumpSettings.DownloadServiceHttpClientTimeout)
				.AddTransientHttpErrorPolicy(builder => builder
					.OrInner<ObjectDisposedException>()
					.WaitAndRetryAsync(superDumpSettings.DownloadServiceRetry,
						_ => TimeSpan.FromMilliseconds(superDumpSettings.DownloadServiceRetryTimeout)));

			// register repository as singleton
			services.AddSingleton<SuperDumpRepository>();

			services.AddSingleton<BundleRepository>();
			services.AddSingleton<IBundleStorage, BundleStorageFilebased>();
			services.AddSingleton<DumpRepository>();
			services.AddSingleton<IDumpStorage, DumpStorageFilebased>();
			services.AddSingleton<AnalyzerPipeline>();
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
			services.AddSingleton<IRelationshipStorage, RelationshipStorageFilebased>();
			services.AddSingleton<IIdenticalDumpStorage, IdenticalDumpStorageFilebased>();
			services.AddSingleton<IdenticalDumpRepository>();
			services.AddSingleton<IJiraApiService, JiraApiService>();
			services.AddSingleton<IJiraIssueStorage, JiraIssueStorageFilebased>();
			services.AddSingleton<JiraIssueRepository>();
			services.AddSingleton<SearchService>();
			if (superDumpSettings.UseAmazonSqs) {
				services.AddSingleton<AmazonSqsClientService>();
				services.AddSingleton<AmazonSqsPollingService>();
			}

			if (superDumpSettings.UseAmazonSqs) {
				services.AddSingleton<IFaultReportSender, AmazonSqsFaultReportingSender>();
			} else {
				services.AddSingleton<IFaultReportSender, ConsoleFaultReportSender>();
			}
			services.AddSingleton<FaultReportingService>();
			
			var sdk = OneAgentSdkFactory.CreateInstance();
			sdk.SetLoggingCallback(new DynatraceSdkLogger(services.BuildServiceProvider().GetService<ILogger<DynatraceSdkLogger>>()));
			services.AddSingleton<IOneAgentSdk>(sdk);

			services.AddWebSocketManager();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, 
				IOptions<SuperDumpSettings> settings, 
				IServiceProvider serviceProvider, 
				SlackNotificationService sns, 
				IAuthorizationHelper authorizationHelper, 
				ILoggerFactory loggerFactory) {
			Task.Run(async () => await app.ApplicationServices.GetService<BundleRepository>().Populate());
			Task.Run(async () => await app.ApplicationServices.GetService<RelationshipRepository>().Populate());
			Task.Run(async () => await app.ApplicationServices.GetService<IdenticalDumpRepository>().Populate());
			if (settings.Value.UseJiraIntegration) {
				Task.Run(async () => await app.ApplicationServices.GetService<JiraIssueRepository>().Populate());
			}

			if (settings.Value.UseHttpsRedirection) {
				app.UseHttpsRedirection();
			}

			app.UseStaticFiles();
			app.UseRouting();

			if (settings.Value.UseLdapAuthentication) {
				app.UseAuthentication();
				app.UseAuthorization();
				app.UseSwaggerAuthorizationMiddleware(authorizationHelper);
			} else {
				app.UseAuthorization();
				app.MapWhen(context => context.Request.Path.StartsWithSegments("/Login") || context.Request.Path.StartsWithSegments("/api/Token"),
					appBuilder => appBuilder.Run(async context => {
						context.Response.StatusCode = 404;
						await context.Response.WriteAsync("");
					}));
			}

			if (settings.Value.UseAllRequestLogging) {
				ILogger logger = loggerFactory.CreateLogger("SuperDumpServiceRequests");
				app.Use(async (context, next) => {
					logger.LogRequest(context);
					await next.Invoke();
				});
			}

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
			if (settings.Value.UseAmazonSqs) {
				app.UseHangfireServer(new BackgroundJobServerOptions {
					Queues = new[] { "amazon-sqs-poll" },
					WorkerCount = 2
				});
				AmazonSqsPollingService amazonSqsService = app.ApplicationServices.GetService<AmazonSqsPollingService>();
				amazonSqsService.StartHangfireJob();
			}

			GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });

			app.UseSwagger();
			app.UseSwaggerUI(c => {
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
			});
			app.UseHealthChecks("/healthcheck");

			LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider());

			if (env.EnvironmentName == "Development") {
				app.UseDeveloperExceptionPage();
				BrowserLinkExtensions.UseBrowserLink(app); // using the extension method directly somehow did not work in .NET Core 2.0 (ambiguous extension method)
			} else {
				app.UseExceptionHandler("/Home/Error");
			}

			app.UseWebSockets();
			app.MapWebSocketManager("/cmd", serviceProvider.GetService<WebTermHandler>());

			//app.UseMvcWithDefaultRoute();
			//app.UseMvc();
			app.UseEndpoints(endpoints => {
				endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
			});
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

	public class DynatraceSdkLogger : ILoggingCallback {
		private readonly ILogger _logger;

		public DynatraceSdkLogger(ILogger logger) {
			this._logger = logger;
		}

		public void Error(string message) {
			_logger.LogError("DynatraceSdk: " + message);
		}

		public void Warn(string message) {
			_logger.LogWarning("DynatraceSdk: " + message);
		}
	}
}