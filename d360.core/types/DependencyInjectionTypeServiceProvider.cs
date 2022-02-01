namespace d360.core.types
{
    public class DependencyInjectionTypeServiceProvider : ITypeServiceProvider
    {
        public IDateTimeService DateTimeService { get; }

        public IDecimalService DecimalService { get; }

        public IInt64Service Int64Service { get; }

        public IInt32TypeService Int32 { get; }

        public DependencyInjectionTypeServiceProvider(IDateTimeService dateTimeService, IDecimalService decimalService, IInt64Service int64Service, IInt32TypeService int32TypeService)
        {
            DateTimeService = dateTimeService;
            DecimalService = decimalService;
            Int64Service = int64Service;
            Int32 = int32TypeService;
        }
    }
}
