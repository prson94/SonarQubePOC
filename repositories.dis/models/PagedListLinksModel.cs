using System;
using System.Collections.Generic;
using System.Text;

namespace repositories.dis.models
{
	public class PagedListLinksModel
	{
		public string previous { get; set; }
		public string self { get; set; }
		public string next { get; set; }
	}
}
