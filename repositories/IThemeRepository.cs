using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using d360.core.entities;

namespace repositories
{
    public interface IThemeRepository
    {
		Task<HttpStatusCode> Delete(Guid uid, Theme theme);

        Task<GetTheme> GetCurrentThemeByUserAsync(ThemewithResource dbTheme);
        
        Task<bool> MarkThemeAsCurrentAsync(Theme theme, Guid uid);

		Task<HttpStatusCode> PostThemeAsync(Theme theme, bool validationOnly = false);

		Task<HttpStatusCode> PutThemeAsync(Theme existingTheme, Theme nowPreviousTheme);

		Task<Uri> GetBaseUriTheme();
	}
}
