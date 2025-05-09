using d360.core.entities;
using System;
using System.Threading.Tasks;

namespace d360.web.Models.Theme
{
	public interface IThemeManager
	{
		Task<Uri> GetBaseUriTheme();

		Task<GetTheme> GetCurrentThemeByUserAsync(ThemewithResource dbTheme, int CurrentCompanyId);
	}
}