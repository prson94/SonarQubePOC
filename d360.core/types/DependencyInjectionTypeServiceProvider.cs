namespace d360.core.types
{
    public class DependencyInjectionTypeServiceProvider : ITypeServiceProvider
    {
        public IDateTimeService DateTimeService { get; }

        public IDecimalService DecimalService { get; }

        public IInt64Service Int64Service { get; }

        public DependencyInjectionTypeServiceProvider(IDateTimeService dateTimeService, IDecimalService decimalService, IInt64Service int64Service)
        {
            DateTimeService = dateTimeService;
            DecimalService = decimalService;
            Int64Service = int64Service;
        }
    }
}