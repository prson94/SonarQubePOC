using System;

namespace d360.core.types
{
    [Obsolete("You should inject ITypeServiceProvider instead of using instance StaticTypeServiceProvider")]
    public class StaticTypeServiceProvider : ITypeServiceProvider
    {
        private static readonly Lazy<IDateTimeService> DateTimeLazy = new Lazy<IDateTimeService>(() => new DateTimeService());
        private static readonly Lazy<IDecimalService> DecimalLazy = new Lazy<IDecimalService>(() => new DecimalService());
        private static readonly Lazy<IInt64Service> Int64Lazy = new Lazy<IInt64Service>(() => new Int64Service());
        private static readonly Lazy<IInt32TypeService> Int32Lazy = new Lazy<IInt32TypeService>(() => new Int32TypeService());

        public IDateTimeService DateTimeService => DateTimeLazy.Value;

        public IDecimalService DecimalService => DecimalLazy.Value;

        public IInt64Service Int64Service => Int64Lazy.Value;

        public IInt32TypeService Int32 => Int32Lazy.Value;
    }
}