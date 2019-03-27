using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	/// <summary>
	/// Datastructure that Jira returns on authentication
	/// </summary>
	public class SessionInfo {
		public Session session;
		public LoginInfo loginInfo;

		public class Session {
			public string name;
			public string value;
		}

		public class LoginInfo {
			public int failedLoginCount;
			public int loginCount;
			public DateTime lastFailedLoginTime;
			public DateTime previousLoginTime;
		}
	}

	public class JiraApiService : IJiraApiService {
		private const string JsonMediaType = "application/json";
		private const string JiraIssueFields = "status,resolution";
		private readonly string[] JiraIssueFieldsArray = JiraIssueFields.Split(",");
		private readonly SemaphoreSlim authSync = new SemaphoreSlim(1, 1);

		private readonly JiraIntegrationSettings settings;
		private readonly HttpClient client;

		public CookieContainer Cookies {
			get { return HttpClientHandler.CookieContainer; }
			set { HttpClientHandler.CookieContainer = value; }
		}

		public HttpClientHandler HttpClientHandler { get; set; }

		public SessionInfo Session { get; set; }

		public JiraApiService(IOptions<SuperDumpSettings> settings) {
			this.settings = settings.Value.JiraIntegrationSettings;
			if (this.settings == null) return;

			HttpClientHandler = new HttpClientHandler {
				AllowAutoRedirect = true,
				UseCookies = true,
				CookieContainer = new CookieContainer()
			};

			client = new HttpClient(HttpClientHandler);
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(JsonMediaType));
		}

		private async Task Authenticate() {
			using (var authClient = new HttpClient()) {
				var uriBuilder = new UriBuilder(settings.JiraApiAuthUrl);
				var response = await authClient.PostAsJsonAsync(settings.JiraApiAuthUrl, new {
					username = this.settings.JiraApiUsername,
					password = this.settings.JiraApiPassword
				});
				var sessionInfo = await response.Content.ReadAsAsync<SessionInfo>();
				this.Session = sessionInfo;
				var cookieDomain = new Uri(new Uri(settings.JiraApiAuthUrl).GetLeftPart(UriPartial.Authority));
				this.Cookies.Add(cookieDomain, new Cookie(sessionInfo.session.name, sessionInfo.session.value));
			}
		}

		private async Task EnsureAuthentication() {
			if (ShallAuthenticate()) {
				await authSync.WaitAsync().ConfigureAwait(false);
				try {
					if (ShallAuthenticate()) {
						await Authenticate();
					}
				} finally {
					authSync.Release();
				}
			}
		}

		private bool ShallAuthenticate() {
			// reauthenticate every 10 minutes
			return Session == null || (DateTime.Now - Session.loginInfo.previousLoginTime).Minutes > 10;
		}

		public async Task<IEnumerable<JiraIssueModel>> GetJiraIssues(string bundleId) {
			return await JiraSearch($"text ~ {bundleId}");
		}

		public async Task<IEnumerable<JiraIssueModel>> GetBulkIssues(IEnumerable<string> issueKeys) {
			if (!issueKeys.Any()) {
				throw new ArgumentException("The issue key enumerable must contain at least one element");
			}
			return await JiraPostSearch($"key in ({string.Join(",", issueKeys)})");
		}

		private async Task<IEnumerable<JiraIssueModel>> JiraSearch(string queryString) {
			var uriBuilder = new UriBuilder(settings.JiraApiSearchUrl);

			NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
			query["jql"] = queryString;
			query["fields"] = JiraIssueFields;
			uriBuilder.Query = query.ToString();

			await EnsureAuthentication();
			return await HandleResponse(await client.GetAsync(uriBuilder.ToString()));
		}

		private async Task<IEnumerable<JiraIssueModel>> JiraPostSearch(string queryString, int retry = 3) {
			await EnsureAuthentication();
			return await HandleResponse(await client.PostAsJsonAsync(settings.JiraApiSearchUrl, new {
				jql = queryString,
				fields = JiraIssueFieldsArray
			}));
		}

		private async Task<IEnumerable<JiraIssueModel>> HandleResponse(HttpResponseMessage response) {
			if (!response.IsSuccessStatusCode) {
				throw new HttpRequestException($"Jira api call {response.RequestMessage.RequestUri} returned status code {response.StatusCode}");
			}
			IEnumerable<JiraIssueModel> issues = (await response.Content.ReadAsAsync<JiraSearchResultModel>()).Issues;
			foreach (JiraIssueModel issue in issues) {
				issue.Url = settings.JiraIssueUrl + issue.Key;
			}
			return issues;
		}

		private AuthenticationHeaderValue GetBasicAuthenticationHeader(string username, string password) {
			return AuthenticationHeaderValue.Parse("Basic " +
				Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
		}
	}
}
