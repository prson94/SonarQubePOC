using System;

namespace d360.core.exceptions
{
	public class InfrastructureException: Exception
	{
		public string InfrastructureComponent { get; set; }

		public InfrastructureException(string message, string component): base(message)
		{
			InfrastructureComponent = component;
		}
	}
}
