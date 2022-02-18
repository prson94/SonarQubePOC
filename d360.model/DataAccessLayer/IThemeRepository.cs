using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IThemeRepository
    {
        HttpStatusCode Delete(Guid uid);
        Task<List<GetTheme>> GetThemesAsync(IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null);
        Task<GetTheme> GetCurrentThemeByUserAsync();
        Theme GetThemeByUid(Guid uid);
        string GetCurrentThemeCustomCssByUser();
        Task<GetTheme> PostThemeAsync(PostTheme theme);
        Task<GetTheme> PutThemeAsync(Guid uid, PutTheme theme);
    }
}