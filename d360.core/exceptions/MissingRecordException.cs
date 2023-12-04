using System;

namespace d360.core.exceptions
{
	public class MissingRecordException: Exception
	{
		public string Table { get; set; }
		public string Identifier { get; set; }
		public MissingRecordException(string table, string identifier,string message): base(message)
		{
			Table = table;
			Identifier = identifier;
		}
	}
}
