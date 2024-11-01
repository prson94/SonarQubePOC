using d360.core.entities;
using Dapper;
using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class History : Repository, IHistory
	{
		public History(DapperConnectionProvider provider) : base(provider) { }
	}
}
