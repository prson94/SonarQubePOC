using System.Collections.Generic;

namespace repositories.dis.models
{
	public class PagesListModel<T>
	{
		public ICollection<T> data { get; set; }
		public PagedListLinksModel links { get; set; }
	}
}
