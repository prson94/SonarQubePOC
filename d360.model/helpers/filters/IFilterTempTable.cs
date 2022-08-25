using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace d360.model.helpers.filters
{
	public interface ITempTableFilter
	{
		AdvancedFilterTempTableInfo GetTempTableFilterData();
	}

	public class AdvancedFilterTempTableInfo
	{
		public string ApiName { get; set; }
		public string TempTableQuery { get; set; }

		public string TempTableJoin { get; set; }
	}

	public class AdvancedFilterTempTableFilters
	{
		private List<AdvancedFilterTempTableInfo> _tables { get; set; } = new List<AdvancedFilterTempTableInfo>();

		private List<AdvancedFilterTempTableInfo> Tables
		{
			get
			{
				return _tables.Distinct().ToList();
			}
		}

		public void Add(AdvancedFilterTempTableInfo data)
		{
			_tables.Add(data);
		}

		public string JoinFilter()
		{
			if (Tables.Count() == 0)
			{
				return string.Empty;
			}
			var sb = new StringBuilder();
			foreach (var t in _tables)
			{
				sb.AppendLine(t.TempTableJoin);
			}
			return sb.ToString();
		}

		public string TempTableSQL()
		{
			if (Tables.Count() == 0)
			{
				return string.Empty;
			}
			var sb = new StringBuilder();
			foreach (var t in _tables)
			{
				sb.AppendLine(t.TempTableQuery);
			}
			return sb.ToString();
		}
	}
}
