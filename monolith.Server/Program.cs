using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using monolith.Server;
using monolith.Server.Models;
using monolith.Server.Services;
using monolith.Server.Utils;
using repositories;
using repositories.azure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder
	.AddGovernCors()
	.AddGovernConfiguration()
	.AddGovernOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICommunity>(o => {
	var config = o.GetRequiredService<AppSettings>();
	return new Community(config.CommunityConnectionString, config.CommunityConnectionString);
});

builder.AddWorkspaceContext();	// Gets the Govern workspace.

builder.Services.AddScoped(c =>
{
	var community = c.GetRequiredService<ICommunity>();
	var workspaceContext = c.GetRequiredService<WorkspaceContext>();
	var connectionString = "";
	if (community != null)
	{
		connectionString = community.GetConnectionStringForTenant(workspaceContext.GovernCompanyId);
	}
	return new DapperConnectionProvider { ReadOnlyConnectionString = connectionString, ReadWriteConnectionString = connectionString };
});

builder.Services.AddScoped<ICatalog, Catalog>();
//builder.Services.AddScoped<ICatalog, repositories.dis.Catalog>();
builder.Services.ConfigureHttpJsonOptions(j =>
{
	j.SerializerOptions.Converters.Add(
		new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
	);
	j.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
// Authentication
builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddCookie(c =>
	{
		c.LoginPath = "/login";
		c.AccessDeniedPath = "/denied";
		c.SlidingExpiration = true;
	})
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = "https://localhost:7208/",
			ValidAudience = "https://localhost:7208/",
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("superSecretKey@1;superSecretKey@1;superSecretKey@1; This is so olleklrj kr kltjhkj thjk yyhkrtjhy;fdsfrwerrwerewrwerwqeqqwe"))
		};
	});

builder.Services.AddAuthorization(o =>
{
	var defaultPolicy = new AuthorizationPolicyBuilder(
		JwtBearerDefaults.AuthenticationScheme,
		CookieAuthenticationDefaults.AuthenticationScheme);
	
	defaultPolicy = defaultPolicy.RequireAuthenticatedUser();
	
	o.DefaultPolicy = defaultPolicy.Build();
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("All");
app.UseAuthentication();
app.UseAuthorization();

var v2 = app.MapGroup("/api/v2").RequireAuthorization();
v2.MapAssetEndpoints();

app.MapGet("sso", () =>
{
	// Determine the type of authentication we should perform 
	// based on the configuration of the workspace.
});

//Forms-authentication.
app.MapPost("sso", () =>
{
	// Validate the forms credentials against Community.

	// Create Cookie.
});

// SAML authentication
app.MapPost("sso/acs", () =>
{
	// Validate the SAML response.

	// Create Cookie.
});

// OIDC authentication
app.MapPost("sso/openid", (monolith.Server.Models.LoginRequest loginRequest) =>
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

app.MapPost("login/cookie", async (IHttpContextAccessor httpContext, monolith.Server.Models.LoginRequest loginRequest) => {
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

app.MapFallbackToFile("/index.html");

app.Run();