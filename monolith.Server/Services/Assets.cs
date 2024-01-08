using d360.core.entities;
using Microsoft.OpenApi.Models;
using repositories;

namespace monolith.Server.Services
{
	public static class Assets
    {
		public static async Task CreateAssets(Guid assetTypeUid)
		{
			await Task.CompletedTask;
		}

		public static async Task ReadAssets(Guid assetTypeUid)
		{
			await Task.CompletedTask;
		}

		public static async Task RemoveAssets(Guid assetTypeUid)
		{
			await Task.CompletedTask;
		}

		public static async Task UpdateAssets(Guid assetTypeUid)
		{
			await Task.CompletedTask;
		}

		public static async Task CreateAssetTypes()
		{
			await Task.CompletedTask;
		}

		public static async Task<IResult> ReadAssetTypes(IEnumerable<ICatalog> catalogs)
		{
			var repository = catalogs.Single(o => o.Platform == Platform.Azure);
			var results = await repository.ReadAssetTypes(0, 100);
			return TypedResults.Ok(results);
		}

		public static async Task RemoveAssetTypes()
		{
			await Task.CompletedTask;
		}

		public static async Task UpdateAssetTypes()
		{
			await Task.CompletedTask;
		}

		public static RouteGroupBuilder MapAssetEndpoints(this RouteGroupBuilder root) 
        {
			var group = root.MapGroup("assets"); //.WithGroupName("Catalog")

			group.MapDelete("", RemoveAssetTypes)
				.WithName("Remove_Asset_Types")
				.WithOpenApi(o => {
					o.Description = "";
					o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

					return o;
				});

			group.MapGet("types", ReadAssetTypes)
				.WithName("Get_Asset_Types")
				.WithOpenApi(o =>
				{
					o.Description = "";
					o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

					return o;
				});

			group.MapPost("", CreateAssetTypes)
				.WithName("Create_Asset_Types")
				.WithOpenApi(o =>
				{
					o.Description = "";
					o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

					return o;
				});

			group.MapPut("", UpdateAssetTypes)
				.WithName("Update_Asset_Types")
				.WithOpenApi(o =>
				{
					o.Description = "";
					o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

					return o;
				});


			var assetsGroup = group.MapGroup("{assetTypeUid:Guid}");
			
			assetsGroup.MapDelete("", RemoveAssets)
				.WithName("Remove_Assets")
				.WithOpenApi(o =>
				{
					o.Description = "";
					o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

					return o;
				});

			assetsGroup.MapGet("", ReadAssets)
				.WithName("Get_Assets")
				.WithOpenApi(o =>
				{
					o.Description = "";
					o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

					return o;
				});

			assetsGroup.MapPost("", CreateAssets)
				.WithName("Create_Assets")
				.WithOpenApi(o =>
				{
					o.Description = "";
					o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

					return o;
				});

			assetsGroup.MapPut("", UpdateAssets)
				.WithName("Update_Assets")
				.WithOpenApi(o =>
				{
					o.Description = "";
					o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

					return o;
				});

			//group.MapGet("{assetTypeUid}/possibleCreators", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("{assetTypeUid}/possibleOwners", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("{assetTypeUid}/possibleRedactors", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("{assetTypeUid}/watchers", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("asset/{assetUid}", (Guid assetUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("asset/{assetUid}/watchers", (Guid assetUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapDelete("batch/{assetTypeUid}", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapPost("batch/{assetTypeUid}", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapPut("batch/{assetTypeUid}", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("classes", () =>
			//{

			//})
			//    .WithName("GetAssetTypeClassesAsync")
			//    .Produces<dynamic>(200)
			//    .WithOpenApi(o => {
			//        o.Description = "Retrieves a list of all asset types classes.";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("colors", () =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("count/{assetTypeUid}", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("counts", () =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("counts/byAssetType", () =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("export/{assetTypeUid}", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("fields/{assetTypeUid}", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapPost("paths", () =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("paths/{assetTypeUid}", (Guid assetTypeUid) =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("possibleSiteNav", () =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapDelete("tags", () =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapPost("tags", () =>
			//{

			//})
			//    .WithName("RemoveAssetTypes")
			//    .WithOpenApi(o => {
			//        o.Description = "";
			//        o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });

			//        return o;
			//    });

			//group.MapGet("types", () =>
			//         {

			//         })
			//             .WithName("GetAssetTypesAsync")
			//             .Produces(200)
			//             .ProducesProblem(400)
			//             .WithOpenApi(o => {
			//                 o.Description = "Retrieves a list of asset types.";
			//                 o.Parameters.Add(new OpenApiParameter { Name = "UseAsTransformation", In = ParameterLocation.Query, Description = "Filter results by Use As Transformation setting.This filter is used to show only Business and Technical asset types which have been marked as transformational asset types in their configuration.Transformational assets have special meaning in the asset browser.Please see the Govern user guide for further details about transformational assets." });
			//        o.Parameters.Add(new OpenApiParameter { Name = "Hierarchical", In = ParameterLocation.Query, Description = "Filter results by Hierarchical setting. This value is used to show Model and Policy Types." });
			//        o.Parameters.Add(new OpenApiParameter { Name = "AutoDisplayParent", In = ParameterLocation.Query, Description = "Filter results by AutoDisplayParent setting. The value is used by the Govern UI to display or hide the parent column on the data grids." });
			//        o.Parameters.Add(new OpenApiParameter { Name = "IncludeLevels", In = ParameterLocation.Query, Description = "Include values of Level, Name, Description properties of the AssetTypeLevel table in response model." });
			//        o.Parameters.Add(new OpenApiParameter { Name = "IncludeDashboardFlag", In = ParameterLocation.Query, Description = "Include value of HasDashboards property of the Report table in response model." });
			//        o.Parameters.Add(new OpenApiParameter { Name = "IncludeUpdatedAndCreatedFields", In = ParameterLocation.Query, Description = "Include values of CreatedOn, CreatedBy, UpdatedOn and UpdatedBy fields." });
			//        o.Parameters.Add(new OpenApiParameter { Name = "IncludeCustomExportTemplatesFlag", In = ParameterLocation.Query, Description = "Include value of HasCustomExportTemplates in response model." });
			//        o.Parameters.Add(new OpenApiParameter { Name = "IncludeHasV2Workflows", In = ParameterLocation.Query, Description = "Include value of IncludeHasV2Workflows in response model." });

			//                 return o;
			//             });

			//group.MapGet("types/{assetTypeUid}/ancestry", (Guid assetTypeUid) =>
			//{
			//})
			//.WithName("RemoveAssetTypes")
			//.WithOpenApi(o => {
			//    o.Description = "";
			//    o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });
			//    return o;
			//});

			//group.MapGet("watchers/counts", () =>
			//{
			//})
			//.WithName("RemoveAssetTypes")
			//.WithOpenApi(o => {
			//    o.Description = "";
			//    o.Parameters.Add(new OpenApiParameter { In = ParameterLocation.Query });
			//    return o;
			//});

			return root;
        }
    }
}
