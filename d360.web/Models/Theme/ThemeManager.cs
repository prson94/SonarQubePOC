using d360.core.entities;
using d360.extensions;
using System;
using System.Threading.Tasks;

namespace d360.web.Models.Theme
{
	public class ThemeManager : IThemeManager
	{
		internal IQueueSource Queue;
		internal IStorageProvider Storage;

		public ThemeManager(
			IQueueSource queue,
			IStorageProvider storage)
		{
			Queue = queue;
			Storage = storage;
		}

		public async Task<GetTheme> GetCurrentThemeByUserAsync(ThemewithResource dbTheme, int CurrentCompanyId)
		{
			var baseUri = await GetBaseUriTheme();

			return dbTheme.ToGetModel(baseUri, CurrentCompanyId);
		}

		public async Task<Uri> GetBaseUriTheme()
		{
			Uri baseUri = null;

			await Task.Run(() => baseUri = Storage.GetBaseUri("themes"));

			return baseUri;
		}
	}
}