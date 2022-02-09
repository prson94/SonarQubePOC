namespace d360.core.types
{
    /// <summary>
    /// Provides access to all type services.
    /// </summary>
    public interface ITypeServiceProvider
    {
        IDateTimeService DateTimeService { get; }

        IDecimalService DecimalService { get; }

        IInt64Service Int64Service { get; }

        IInt32TypeService Int32 { get; }
    }
}
