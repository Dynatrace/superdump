﻿using System;
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
		private const string JiraIssueFields = "status";
		private readonly string[] JiraIssueFieldsArray = JiraIssueFields.Split(",");

		private readonly SuperDumpSettings settings;
		private readonly HttpClient client;

		public JiraApiService(IOptions<SuperDumpSettings> settingOptions) {
			settings = settingOptions.Value;

			client = new HttpClient();
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(JsonMediaType));
			client.DefaultRequestHeaders.Authorization = GetBasicAuthenticationHeader(settings.JiraApiUsername, settings.JiraApiPassword);
		}

		public async Task<IEnumerable<JiraIssueModel>> GetJiraIssues(string bundleId) {
			return await JiraSearch($"text ~ {bundleId}");
		}

		public async Task<IEnumerable<JiraIssueModel>> GetBulkIssues(IEnumerable<string> issueKeys) {
			return await JiraPostSearch($"key in ({string.Join(",", issueKeys)})");
		}

		private async Task<IEnumerable<JiraIssueModel>> JiraSearch(string queryString) {
			var uriBuilder = new UriBuilder(settings.JiraApiSearchUrl);

			NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
			query["jql"] = queryString;
			query["fields"] = JiraIssueFields;
			uriBuilder.Query = query.ToString();

			string resultString = await client.GetStringAsync(uriBuilder.ToString());
			return JsonConvert.DeserializeObject<JiraSearchResultModel>(resultString).Issues;
		}

		private async Task<IEnumerable<JiraIssueModel>> JiraPostSearch(string queryString) {
			HttpResponseMessage response = await client.PostAsJsonAsync(settings.JiraApiSearchUrl, new {
				jql = queryString,
				fields = JiraIssueFieldsArray
			});
			response.EnsureSuccessStatusCode();
			
			return (await response.Content.ReadAsAsync<JiraSearchResultModel>()).Issues;
		}

		private AuthenticationHeaderValue GetBasicAuthenticationHeader(string username, string password) {
			return AuthenticationHeaderValue.Parse("Basic " +
				Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
		}
	}
}