using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Services;

namespace SuperDumpService.Controllers.api {
	[Route("api/[controller]")]
	[ApiController]
	public class TokenController : ControllerBase {
		private readonly LdapAuthentcationService authentcationService;
		private readonly LdapAuthenticationSettings settings;
		private readonly ILogger<TokenController> logger;

		public TokenController(LdapAuthentcationService authentcationService, IOptions<SuperDumpSettings> settings, ILoggerFactory loggerFactory) {
			this.authentcationService = authentcationService;
			this.settings = settings.Value.LdapAuthenticationSettings;
			logger = loggerFactory.CreateLogger<TokenController>();
		}

		[HttpPost]
		public IActionResult Post(ApiLoginModel loginModel) {
			try {
				ClaimsPrincipal userPrincipal = authentcationService.ValidateAndGetUser(loginModel.Username, loginModel.Password);

				logger.LogLoginEvent("Api token requested", HttpContext, loginModel.Username);

				return Ok(new {
					token = new JwtSecurityTokenHandler().WriteToken(
				new JwtSecurityToken(
					issuer: settings.TokenIssuer,
					audience: settings.TokenAudience,
					claims: userPrincipal.Claims,
					expires: DateTime.UtcNow.AddDays(settings.TokenExpireTimeInDays),
					signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(settings.TokenSigningKey)), SecurityAlgorithms.HmacSha256)
				))
				});
			} catch (InvalidCredentialException) {
				logger.LogFailedLogin("Api token request with invalid credentials", HttpContext, loginModel.Username);
				return Unauthorized();
			} catch (UnauthorizedAccessException) {
				logger.LogFailedLogin("Api token request with missing Permissions", HttpContext, loginModel.Username);
				return Unauthorized();
			}
		}
	}
}