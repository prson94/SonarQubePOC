using System;

namespace d360.core.types
{
    [Obsolete("You should inject ITypeServiceProvider instead of using instance StaticTypeServiceProvider")]
    public class StaticTypeServiceProvider : ITypeServiceProvider
    {
        private static readonly Lazy<IDateTimeService> DateTimeLazy = new Lazy<IDateTimeService>(() => new DateTimeService());
        private static readonly Lazy<IDecimalService> DecimalLazy = new Lazy<IDecimalService>(() => new DecimalService());
        private static readonly Lazy<IInt64Service> Int64Lazy = new Lazy<IInt64Service>(() => new Int64Service());

        public IDateTimeService DateTimeService => DateTimeLazy.Value;

        public IDecimalService DecimalService => DecimalLazy.Value;

        public IInt64Service Int64Service => Int64Lazy.Value;
    }
}