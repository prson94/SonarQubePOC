using System.Diagnostics;

namespace d360.web.Utilities
{
	internal sealed class RuntimeInfo : IRuntimeInfo
	{

		public bool IsReleaseBuild
		{
			get
			{
#if DEBUG
				return false;
#else
				return true;
#endif
			}
		}

		public bool IsDebuggerAttached => Debugger.IsAttached;
	}
}
