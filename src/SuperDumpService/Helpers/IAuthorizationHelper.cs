using System.Security.Claims;

namespace SuperDumpService.Helpers {
	public interface IAuthorizationHelper {
		bool CheckPolicy(ClaimsPrincipal user, string policy);
	}
}
