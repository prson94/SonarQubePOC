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

		Task<List<GetTheme>> GetThemesAsync(List<Theme> theme, Guid themeUid, CancellationToken? cancellationToken = null);
        
        Task<GetTheme> GetCurrentThemeByUserAsync(Theme dbTheme);
        
        Task<bool> MarkThemeAsCurrentAsync(Theme theme, Guid uid);
        
        Task<GetTheme> PostThemeAsync(Theme theme, bool validationOnly = false);

		Task<GetTheme> PutThemeAsync(Guid uid, PutTheme theme, Theme existingTheme, Theme nowPreviousTheme);

	}
}
