using System.Security.Claims;

namespace SuperDumpService.Helpers {
	public class NoAuthorizationHelper : IAuthorizationHelper {
		public bool CheckPolicy(ClaimsPrincipal user, string policy) {
			return true;
		}
	}
}
