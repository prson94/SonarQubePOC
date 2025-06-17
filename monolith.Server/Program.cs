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

app.MapGroup("").MapAuthenticationEndpoints();

app.MapFallbackToFile("/index.html");

app.Run();