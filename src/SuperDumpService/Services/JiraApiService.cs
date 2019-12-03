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
using Newtonsoft.Json.Serialization;
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
		private static readonly JsonSerializerSettings CamelCaseJsonSettings =
			new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

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
			return Session == null || Session.loginInfo == null || (DateTime.Now - Session.loginInfo.previousLoginTime).Minutes > 10;
		}

		public async Task<IEnumerable<JiraIssueModel>> GetJiraIssues(string bundleId) {
			return await JiraPostSearch($"text ~ {bundleId}");
		}

		public async Task<IEnumerable<JiraIssueModel>> GetBulkIssues(IEnumerable<string> issueKeys) {
			if (!issueKeys.Any()) {
				throw new ArgumentException("The issue key enumerable must contain at least one element");
			}
			return await JiraPostSearch($"key in ({string.Join(",", issueKeys)})");
		}

		private async Task<IEnumerable<JiraIssueModel>> JiraPostSearch(string queryString, int retry = 3) {
			await EnsureAuthentication();
			var query = new JiraSearchQuery {
				Jql = queryString,
				Fields = JiraIssueFieldsArray,
				StartAt = 0
			};

			JiraSearchResultModel searchResult;
			List<JiraIssueModel> issues = null;
			//This loop is necessary since the maxResults per query are limited by a Jira setting.
			do {
				searchResult = await HttpPostQuery(query);
				if (issues == null) { //Has to be initialized after we know how many results there are to avoid reallocating the list
					issues = new List<JiraIssueModel>(searchResult.Total);
				}
				foreach (JiraIssueModel issue in searchResult.Issues) {
					issue.Url = settings.JiraIssueUrl + issue.Key;
					issues.Add(issue);
				}
				query.StartAt += searchResult.MaxResults;
			} while (query.StartAt < searchResult.Total);
			return issues;
		}

		private async Task<JiraSearchResultModel> HttpPostQuery(JiraSearchQuery query) {
			HttpResponseMessage response = await client.PostAsync(settings.JiraApiSearchUrl,
				new StringContent(JsonConvert.SerializeObject(query, CamelCaseJsonSettings), Encoding.UTF8, "application/json"));

			if (!response.IsSuccessStatusCode) {
				throw new HttpRequestException($"Jira api call {response.RequestMessage.RequestUri} returned status code {response.StatusCode}");
			}
			return await response.Content.ReadAsAsync<JiraSearchResultModel>();
		}

		private AuthenticationHeaderValue GetBasicAuthenticationHeader(string username, string password) {
			return AuthenticationHeaderValue.Parse("Basic " +
				Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
		}
	}
}
