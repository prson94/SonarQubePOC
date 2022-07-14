namespace d360.web.Utilities
{
	public interface IRuntimeInfo
	{
		bool IsReleaseBuild { get; }

		bool IsDebuggerAttached { get; }
	}
}
