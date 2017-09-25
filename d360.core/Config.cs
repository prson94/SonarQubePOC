using System;
using System.ComponentModel;
using System.Configuration;

namespace d360.core
{
    public static class Config
    {
        public static T GetValue<T>(string name)
        {
            var value = ConfigurationManager.AppSettings[name];

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null)
                {
                    return (T)converter.ConvertFromString(value);
                }
                return default(T);
            }
            catch (NotSupportedException)
            {
                return default(T);
            }
        }
    }
}
