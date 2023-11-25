using System;

namespace d360.extensions.search.models
{
	internal class SemanticTypeSqlModel : IPagedQuerySqlModel
	{
		public long AssetID { get; set; }

		public string Name { get; set; }

		public string Qualifier { get; set; }
		public Guid SemanticUID { get; set; }
	}
}
