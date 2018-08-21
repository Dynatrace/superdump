using Microsoft.Extensions.Configuration;

namespace SuperDumpService {
	public class LdapAuthenticationSettings
    {
		public enum ServiceUserMode {
			Integrated,
			UserCredentials,
			ServiceUser
		}

		public string LdapDomain { get; set; }
		public ServiceUserMode LdapServiceUserMode { get; set; }
		public string LdapServiceUserName { get; set; }
		public string LdapServiceUserPwd { get; set; }
		public string AuthenticationCookieName { get; set; }
		public double CookieExpireTimeSpanInDays { get; set; }
		public string[] ViewerDownloadableFiles { get; set; }

		public string TokenIssuer { get; set; }
		public string TokenAudience { get; set; }
		public string TokenSigningKey { get; set; }
		public double TokenExpireTimeInDays { get; set; }

		public IConfigurationSection GroupNames { get; set; }
	}
}
