using System;

namespace d360.core
{
	public class UDTOrderAttribute : Attribute
	{
		public UDTOrderAttribute(int order)
		{
			Order = order;
		}

		public int Order { get; }
	}
}
