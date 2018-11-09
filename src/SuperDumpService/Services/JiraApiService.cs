using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public class JiraApiService {
		private const string JsonMediaType = "application/json";
		private const string JiraIssueFields = "status,resolution";
		private readonly string[] JiraIssueFieldsArray = JiraIssueFields.Split(",");

		private readonly JiraIntegrationSettings settings;
		private readonly HttpClient client;

		public JiraApiService(IOptions<SuperDumpSettings> settings) {
			this.settings = settings.Value.JiraIntegrationSettings;
			if (this.settings == null) return;
			client = new HttpClient();
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(JsonMediaType));
			client.DefaultRequestHeaders.Authorization = GetBasicAuthenticationHeader(this.settings.JiraApiUsername, this.settings.JiraApiPassword);
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

			return await HandleResponse(await client.GetAsync(uriBuilder.ToString()));
		}

		private async Task<IEnumerable<JiraIssueModel>> JiraPostSearch(string queryString) {
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
