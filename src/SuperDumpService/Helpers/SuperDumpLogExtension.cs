using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SuperDumpService.Models;

namespace SuperDumpService.Helpers {
	public static class SuperDumpLogExtension {
		private const string DefaultLogText = "{Text}, Ip: {Ip}, User: {User}";
		private const string BundleLogText = DefaultLogText + ", BundleId: {BundleId}, {customProperties}";
		private const string DumpLogText = BundleLogText + ", DumpId: {DumpId}";
		private const string FileLogText = DumpLogText + ", Filename: {Filename}";
		private const string UploadLogText = BundleLogText + ", Uri: {Uri}";
		private const string SearchLogText = DefaultLogText + ", Filter: {filter}";
		private const string NotFoundText = DefaultLogText + ", {IdentifierDescription}: {Identifier}";
		private const string ElasticCleanText = DefaultLogText + ", Clean: {clean}";
		private const string AccessDeniedText = DefaultLogText + ", Url: {Url}";
		private const string RequestLogText = "Ip: {Ip}, User: {User}, Action: {action}, Path: {path}, Parameters: {params}";
		private const string DumpComparisonText = DefaultLogText + ", BundleId1: {BundleId1}, DumpId1: {DumpId1}, BundleId1: {BundleId1}, DumpId1: {DumpId1}";

		public static void LogDumpAccess(this ILogger logger, string text, HttpContext context, BundleMetainfo bundleInfo, string dumpId) {
			logger.LogInformation(DumpLogText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name,
				bundleInfo.BundleId, GetCustomPropertyString(bundleInfo.CustomProperties), dumpId);
		}

		public static void LogBundleAccess(this ILogger logger, string text, HttpContext context, BundleMetainfo bundleInfo) {
			logger.LogInformation(BundleLogText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name,
				bundleInfo.BundleId, GetCustomPropertyString(bundleInfo.CustomProperties));
		}

		public static void LogFileAccess(this ILogger logger, string text, HttpContext context, BundleMetainfo bundleInfo, string dumpId, string filename) {
			logger.LogInformation(FileLogText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name,
				bundleInfo.BundleId, GetCustomPropertyString(bundleInfo.CustomProperties), dumpId, filename);
		}

		public static void LogFileUpload(this ILogger logger, string text, HttpContext context, string bundleId, IDictionary<string, string> customProperties, string uri) {
			logger.LogInformation(UploadLogText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name,
				bundleId, GetCustomPropertyString(customProperties).ToString(), uri);
		}

		public static void LogSearch(this ILogger logger, string text, HttpContext context, string filter) {
			logger.LogInformation(SearchLogText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name, filter);
		}

		public static void LogNotFound(this ILogger logger, string text, HttpContext context, string identifierDescription, string identifier) {
			logger.LogInformation(NotFoundText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name,
				identifierDescription, identifier);
		}

		public static void LogElasticClean(this ILogger logger, string text, HttpContext context, bool clean) {
			logger.LogInformation(ElasticCleanText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name, clean);
		}
		public static void LogLoginEvent(this ILogger logger, string text, HttpContext context, string username) {
			logger.LogInformation(DefaultLogText, text, context.Connection.RemoteIpAddress.ToString(), username);
		}

		public static void LogFailedLogin(this ILogger logger, string text, HttpContext context, string username) {
			logger.LogWarning(DefaultLogText, text, context.Connection.RemoteIpAddress.ToString(), username);
		}

		public static void LogDefault(this ILogger logger, string text, HttpContext context) {
			logger.LogInformation(DefaultLogText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name);
		}

		public static void LogAccessDenied(this ILogger logger, string text, HttpContext context, string url) {
			logger.LogWarning(AccessDeniedText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name, url);
		}

		public static void LogRequest(this ILogger logger, HttpContext context) {
			logger.LogInformation(RequestLogText,
				context.Connection.RemoteIpAddress.ToString(),
				context.User.Identity != null ? context.User.Identity.Name : "null",
				context.Request.Method, context.Request.Path, context.Request.Query);
		}

		public static void LogAdminEvent(this ILogger logger, string text, HttpContext context) {
			logger.LogInformation(DefaultLogText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name);
		}
		public static void LogSimilarityEvent(this ILogger logger, string text, HttpContext context, string bundleId1, string dumpId1, string bundleId2, string dumpId2) {
			logger.LogInformation(DumpComparisonText, text, context.Connection.RemoteIpAddress.ToString(), context.User.Identity.Name, bundleId1, dumpId1, bundleId2, dumpId2);
		}

		private static string GetCustomPropertyString(IDictionary<string, string> customProperties) {
			return string.Join(", ", customProperties.Select(entry => $"{entry.Key}: {entry.Value}"));
		}
	}
}