using System;

namespace d360.core
{
	public class UDTNameAttribute : Attribute
	{
		public UDTNameAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; }
	}
}
