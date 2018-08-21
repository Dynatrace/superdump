using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Security.Authentication;
using System.Security.Claims;

namespace SuperDumpService.Services {
	public class LdapAuthentcationService {
		public const string LdapAuthenticationType = "Ldap";
		private const ContextOptions PrincipalContextOptions = ContextOptions.SecureSocketLayer | ContextOptions.Negotiate;

		private readonly LdapAuthenticationSettings configuration;

		public Dictionary<string, string> Groups { get; private set; }

		public LdapAuthentcationService(LdapAuthenticationSettings configuration) {
			this.configuration = configuration;

			Groups = new Dictionary<string, string>();
			foreach (var element in configuration.GroupNames.GetChildren()) {
				Groups.Add(element.Key, element.Value);
			}
		}

		public ClaimsPrincipal ValidateAndGetUser(string username, string password) {
			using (PrincipalContext context = CreatePrincipalContext(username, password)) {
				if (!context.ValidateCredentials(username, password, ContextOptions.SecureSocketLayer | ContextOptions.SimpleBind)) {
					throw new InvalidCredentialException();
				}

				var userPrincipal = UserPrincipal.FindByIdentity(context,
					!username.Contains('@') ? IdentityType.SamAccountName : IdentityType.UserPrincipalName, username);
				if (userPrincipal == null) {
					throw new InvalidCredentialException();
				}

				var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
				bool inGroup = false;
				foreach (var group in Groups) {
					if (userPrincipal.IsMemberOf(context, IdentityType.Name, group.Value)) {
						claims.Add(new Claim(ClaimTypes.GroupSid, group.Value));
						inGroup = true;
					}
				}
				if (!inGroup) {
					throw new UnauthorizedAccessException("Your user account does not have access to SuperDump");
				}

				return new ClaimsPrincipal(new List<ClaimsIdentity> { new ClaimsIdentity(claims, LdapAuthenticationType) });
			}
		}

		private PrincipalContext CreatePrincipalContext(string username, string password) {
			switch (configuration.LdapServiceUserMode) {
				case LdapAuthenticationSettings.ServiceUserMode.ServiceUser:
					return new PrincipalContext(ContextType.Domain, configuration.LdapDomain, null,
					PrincipalContextOptions, configuration.LdapServiceUserName, configuration.LdapServiceUserPwd);
				case LdapAuthenticationSettings.ServiceUserMode.UserCredentials:
					return new PrincipalContext(ContextType.Domain, configuration.LdapDomain, null, PrincipalContextOptions, username, password);
				case LdapAuthenticationSettings.ServiceUserMode.Integrated:
					return new PrincipalContext(ContextType.Domain, configuration.LdapDomain, null, PrincipalContextOptions);
				default:
					throw new NotImplementedException();
			}
		}
	}
}
