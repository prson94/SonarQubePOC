using d360.model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace d360.web.Controllers.V2
{
    public class BaseV2ApiController : BaseApiController
    {
        public BaseV2ApiController(ICommunityContext community, ICompanyContext company)
            : base(community,company)
        {            
        }
        
        protected async Task<T> readRequestJsonContent<T>(HttpRequestMessage request, bool deserializeAsIs = false)
        {
            string json = "";

            if (request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new MultipartMemoryStreamProvider();
                await request.Content.ReadAsMultipartAsync(streamProvider);

                json = await streamProvider.Contents.Single().ReadAsStringAsync();
            }
            else
            {
                json = await request.Content.ReadAsStringAsync();
            }

            if(deserializeAsIs) return JsonConvert.DeserializeObject<T>(json);

            if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
                return default(T);
            else
            {
                if ((json.StartsWith("{") && json.EndsWith("}")) || //For object
                        (json.StartsWith("[") && json.EndsWith("]"))) //For array
                {
                    bool isValid = false;
                    try
                    {
                        var obj = JToken.Parse(json);
                        isValid = true;
                        obj = null;
                    }
                    catch
                    {
                        isValid = false;
                    }

                    if (isValid)
                        return JsonConvert.DeserializeObject<T>(json);
                    else
                        return default(T);
                }
                else
                {
                    return default(T);
                }
            }
        }
    }
}
