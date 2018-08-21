namespace SuperDumpService.Models {
	public class LdapAuthenticationPathOptions {
		public string LoginPath { get; set; }
		public string LogoutPath { get; set; }
		public string AccessDeniedPath { get; set; }
	}
}
