using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace monolith.Server.Services
{
	public static class Authentication
	{
		public static RouteGroupBuilder MapAuthenticationEndpoints(this RouteGroupBuilder root)
		{
			var group = root.MapGroup("").WithGroupName("Authentication");

			group.MapGet("sso", () =>
			{
				// Determine the type of authentication we should perform 
				// based on the configuration of the workspace.
			});

			//Forms-authentication.
			group.MapPost("sso", () =>
			{
				// Validate the forms credentials against Community.

				// Create Cookie.
			});

			// SAML authentication
			group.MapPost("sso/acs", () =>
			{
				// Validate the SAML response.

				// Create Cookie.
			});

			// OIDC authentication
			group.MapPost("sso/openid", (monolith.Server.Models.LoginRequest loginRequest) =>
			{
				// Validate the Code / state.

				// Retrieve JWT.

				// Create Cookie.

				// Test code below
				var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("superSecretKey@1;superSecretKey@1;superSecretKey@1; This is so olleklrj kr kltjhkj thjk yyhkrtjhy;fdsfrwerrwerewrwerwqeqqwe"));
				var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
				var tokenOptions = new JwtSecurityToken(
				issuer: "https://localhost:7208/",
				audience: "https://localhost:7208/",
					claims: new List<Claim> { new Claim(ClaimTypes.Name, loginRequest.Username ?? string.Empty) },
					expires: DateTime.Now.AddMinutes(30),
					signingCredentials: signinCredentials
				);
				var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
				return TypedResults.Ok(new { Token = tokenString });
			});

			group.MapPost("login/cookie", async (IHttpContextAccessor httpContext, monolith.Server.Models.LoginRequest loginRequest) => {
				var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
				identity.AddClaim(new Claim(ClaimTypes.Name, loginRequest.Username ?? string.Empty));
				var principal = new ClaimsPrincipal(identity);

				await httpContext.HttpContext.SignInAsync(
					CookieAuthenticationDefaults.AuthenticationScheme,
					principal,
					new AuthenticationProperties
					{
						IsPersistent = true,
						AllowRefresh = true,
						ExpiresUtc = DateTime.UtcNow.AddDays(1)
					});

				return Results.Ok();
			});

			return group;
		}
	}
}
