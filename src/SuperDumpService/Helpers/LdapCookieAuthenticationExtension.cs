using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SuperDumpService.Models;
using SuperDumpService.Services;

namespace SuperDumpService.Helpers {
	public static class LdapCookieAuthenticationExtension {
		public const string SuperDumpAuthenticationScheme = "SuperDumpAuthenticationScheme";

		public const string AdminPolicy = "Admin";
		public const string UserPolicy = "User";
		public const string ViewerPolicy = "Viewer";

		public static void AddLdapCookieAuthentication(this IServiceCollection services, LdapAuthenticationSettings configuration, LdapAuthenticationPathOptions pathOptions) {
			LdapAuthentcationService ldapAuthService = new LdapAuthentcationService(configuration);
			services.AddSingleton(ldapAuthService);
			
			services.AddAuthentication(SuperDumpAuthenticationScheme)
				.AddPolicyScheme(SuperDumpAuthenticationScheme, SuperDumpAuthenticationScheme, options => {
				options.ForwardDefaultSelector = context => context.Request.Path.StartsWithSegments("/api") ? 
						JwtBearerDefaults.AuthenticationScheme : CookieAuthenticationDefaults.AuthenticationScheme;
				})
				.AddCookie(options => {
					options.Cookie.Name = configuration.AuthenticationCookieName;
					options.SlidingExpiration = true;
					options.ExpireTimeSpan = TimeSpan.FromDays(configuration.CookieExpireTimeSpanInDays);
					options.Cookie.HttpOnly = true;
					options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

					options.LoginPath = pathOptions.LoginPath;
					options.LogoutPath = pathOptions.LogoutPath;
					options.AccessDeniedPath = pathOptions.AccessDeniedPath;
				})
				.AddJwtBearer(options =>
					options.TokenValidationParameters = new TokenValidationParameters() {
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true,
						ValidIssuer = configuration.TokenIssuer,
						ValidAudience = configuration.TokenAudience,
						IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.TokenSigningKey))
					}
				);

			services.AddAuthorization(options => {
				string adminGroup = ldapAuthService.Groups[AdminPolicy];
				string userGroup = ldapAuthService.Groups[UserPolicy];
				string viewerGroup = ldapAuthService.Groups[ViewerPolicy];

				options.AddPolicy(AdminPolicy, policy =>
					policy.RequireAssertion(context => context.User.HasClaim(ClaimTypes.GroupSid, adminGroup)));

				options.AddPolicy(UserPolicy, policy =>
					policy.RequireAssertion(context => context.User.HasClaim(claim =>
						claim.Type == ClaimTypes.GroupSid && (claim.Value == adminGroup || claim.Value == userGroup))));

				options.AddPolicy(ViewerPolicy, policy =>
					policy.RequireAssertion(context => context.User.HasClaim(claim =>
						claim.Type == ClaimTypes.GroupSid && (claim.Value == adminGroup || claim.Value == userGroup || claim.Value == viewerGroup))));
			});
		}

		public static void AddPoliciesForNoAuthentication(this IServiceCollection services) {
			services.AddAuthorization(options => {
				options.AddPolicy(LdapCookieAuthenticationExtension.AdminPolicy, policy => policy.RequireAssertion(context => true));
				options.AddPolicy(LdapCookieAuthenticationExtension.UserPolicy, policy => policy.RequireAssertion(context => true));
				options.AddPolicy(LdapCookieAuthenticationExtension.ViewerPolicy, policy => policy.RequireAssertion(context => true));
			});
		}

		public static void UseSwaggerAuthorizationMiddleware(this IApplicationBuilder app, IAuthorizationHelper authorizationHelper) {
			app.Use(async (context, next) => {
				if (context.Request.Path.StartsWithSegments("/swagger") && !authorizationHelper.CheckPolicy(context.User, LdapCookieAuthenticationExtension.ViewerPolicy)) {
					if (context.User.Identity.IsAuthenticated) {
						await context.ForbidAsync();
					} else {
						await context.ChallengeAsync();
					}
				} else {
					await next.Invoke();
				}
			});
		}
	}
}
