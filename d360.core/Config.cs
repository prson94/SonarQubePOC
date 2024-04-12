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

		public static string GetStorageUrl(string folder)
		{
			var storageConnectionString = GetValue<string>(constants.Setting.Storage);
			storageConnectionString = storageConnectionString.Split([';'])[1].Split('=')[1]; // extract the account name out of the storage connection string.
			return $"https://{storageConnectionString}.blob.core.windows.net/{folder}";
		}
    }
}
