using Microsoft.OpenApi.Models;
using monolith.Server.Models;

namespace monolith.Server.Utils
{
	public static class BuilderExtensions
	{
		public static WebApplicationBuilder AddGovernConfiguration(this WebApplicationBuilder builder)
		{
			builder.Services.AddSingleton(builder.Configuration.GetSection("AppSettings").Get<AppSettings>());

			return builder;
		}

		public static WebApplicationBuilder AddGovernCors(this WebApplicationBuilder builder)
		{ 
			builder.Services.AddCors(options =>
			{
				options.AddPolicy(name: "All",
								  builder =>
								  {
									  builder
										.AllowAnyOrigin()
										.AllowAnyMethod()
										.AllowAnyHeader();
								  });
			});

			return builder;
		}

		public static WebApplicationBuilder AddGovernOpenApi(this WebApplicationBuilder builder)
		{
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(o => {

				o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.ApiKey
				});

				o.AddSecurityRequirement(new OpenApiSecurityRequirement {
					{
						new OpenApiSecurityScheme {
							Reference = new OpenApiReference {
								Id = "Bearer",
								Type = ReferenceType.SecurityScheme
							}
						},
						new List<string>()
					}
				});

				o.SwaggerDoc("v1", new OpenApiInfo
				{
					Description = "Data360 Govern Services",
					Version = "v1",
					Title = "Govern",
					Contact = new OpenApiContact
					{
						Name = "Precisely, Inc."
					}
				});
			});

			return builder;
		}

		public static WebApplicationBuilder AddWorkspaceContext(this WebApplicationBuilder builder) 
		{
#if DEBUG
			builder.Services.AddSingleton(
				builder.Configuration.GetSection("DebugWorkspaceContext").Get<WorkspaceContext>()
				);
#else
builder.Services.AddScoped(o =>
{
	var ctx = o.GetRequiredService<IHttpContextAccessor>();
	var host = ctx.HttpContext?.Request.Host.Host;
	//Need to resolve the host to a Govern environment in Community.
	return new WorkspaceContext { };
});
#endif
			return builder;
		}
	}
}
