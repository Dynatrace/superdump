using System;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SuperDumpService.Helpers;
using SuperDumpService.Services;
using SuperDumpService.ViewModels;

namespace SuperDumpService.Controllers {
	[AutoValidateAntiforgeryToken]
	public class LoginController : Controller {
		private readonly LdapAuthentcationService authentificationHelper;
		private readonly ILogger logger;

		public LoginController(LdapAuthentcationService authentificationHelper, ILoggerFactory loggerFactory) {
			this.authentificationHelper = authentificationHelper;
			logger = loggerFactory.CreateLogger<LoginController>();
		}

		public IActionResult Index() {
			return View("Login", new LoginViewModel { ReturnUrl = Request.Query["ReturnUrl"].FirstOrDefault() ?? "/Home/Index" });
		}

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel loginModel) {
			try {
				await HttpContext.SignInAsync(
					authentificationHelper.ValidateAndGetUser(loginModel.Username, loginModel.Password),
						new AuthenticationProperties { IsPersistent = loginModel.RememberMe });
				logger.LogLoginEvent("Successful Login", HttpContext, loginModel.Username);
				return Redirect(loginModel.ReturnUrl);
			} catch (InvalidCredentialException) {
				logger.LogFailedLogin("Failed Login", HttpContext, loginModel.Username);
				loginModel.AlertMessage = "The username or password is incorrect";
				loginModel.Password = string.Empty;
				return View(loginModel);
			} catch (UnauthorizedAccessException) {
				logger.LogFailedLogin("Login with missing Permissions", HttpContext, loginModel.Username);
				loginModel.AlertMessage = "You do not have permission to access SuperDump";
				loginModel.Username = string.Empty;
				loginModel.Password = string.Empty;
				return View(loginModel);
			}
		}

		[HttpPost]
		public async Task<IActionResult> Logout() {
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			return Redirect("/Home/Index");
		}

		public IActionResult AccessDenied() {
			ViewResult result = View();
			result.StatusCode = 401;
			logger.LogAccessDenied("Access Denied", HttpContext, Request.Query["ReturnUrl"].FirstOrDefault());
			return result;
		}
	}
}