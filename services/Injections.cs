using Microsoft.Extensions.DependencyInjection;
using repositories;
using repositories.azure;

namespace services
{
	public static class Injections
	{
		public static IServiceCollection AddServiceLayer(this IServiceCollection services)
		{
			//services.AddScoped<ICatalog, Catalog>();
			//services.AddScoped<ICommunity, Community>();
			//services.AddScoped<IHistory, History>();
			//services.AddScoped<IScoring, Scoring>();
			//services.AddScoped<ISearch, Search>();
			//services.AddScoped<ISocial, Social>();
			//services.AddScoped<IUsage, Usage>();
			//services.AddScoped<IWorkflow, Workflow>();
			//services.AddScoped<IWorkspaces, Workspaces>();
			//services.AddKeyedScoped<IPotion, HealingPotion>("healing"); // Example of adding a keyed service, if needed.

			services.AddScoped<ICatalogService, CatalogService>();
			//services.AddScoped<IHistoryService, HistoryService>();
			//services.AddScoped<IScoringService, ScoringService>();
			//services.AddScoped<IWorkspacesService, WorkspacesService>();
			//services.AddScoped<IQueueSource, QueueSource>();
			//services.AddScoped<ISecurityContextProvider, SecurityContextProvider>();
			//services.AddScoped<IStorageProvider, StorageProvider>();
			return services;
		}
	}
}
