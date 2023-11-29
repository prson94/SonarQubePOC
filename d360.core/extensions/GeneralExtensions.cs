using Newtonsoft.Json;

namespace d360.core
{
	public static class GeneralExtensions
    {
		public static T AsObject<T>(this string item)
		{ 
			return JsonConvert.DeserializeObject<T>(item);
		}

        public static string AsJson<T>(this T item)
        {
            var json = JsonConvert.SerializeObject(item);
            return json;
        }

        public static T CloneThis<T>(this T item)
        {
            var json = JsonConvert.SerializeObject(item);
            T newItem = JsonConvert.DeserializeObject<T>(json);
            return newItem;
        }
    }
}
