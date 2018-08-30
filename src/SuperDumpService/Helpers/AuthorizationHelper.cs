using System.Security.Claims;
using SuperDumpService.Services;

namespace SuperDumpService.Helpers {
	public class AuthorizationHelper : IAuthorizationHelper {
		private readonly LdapAuthentcationService authenticationService;

		public AuthorizationHelper(LdapAuthentcationService authenticationService) {
			this.authenticationService = authenticationService;
		}

		public bool CheckPolicy(ClaimsPrincipal user, string policy) {
			if (!user.Identity.IsAuthenticated) {
				return false;
			}

			return InGroup(user, LdapCookieAuthenticationExtension.AdminPolicy) ||
				policy != LdapCookieAuthenticationExtension.AdminPolicy && InGroup(user, LdapCookieAuthenticationExtension.UserPolicy) ||
				policy == LdapCookieAuthenticationExtension.ViewerPolicy && InGroup(user, LdapCookieAuthenticationExtension.ViewerPolicy);
		}

		private bool InGroup(ClaimsPrincipal user, string groupName) {
			return user.HasClaim(ClaimTypes.GroupSid, authenticationService.Groups[groupName]);
		}
	}
}
