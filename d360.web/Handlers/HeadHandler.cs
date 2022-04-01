using System.Net.Http;
using System.Threading.Tasks;

namespace d360.web.Handlers
{

    public class HeadHandler : DelegatingHandler
    {
        private const string Head = "IsHead";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Head)
            {
                request.Method = HttpMethod.Get;
                request.Properties.Add(Head, true);
            }

            var response = await base.SendAsync(request, cancellationToken);

            try
            {
                if (response.RequestMessage != null)
                {
                    response.RequestMessage.Properties.TryGetValue(Head, out object isHead);

                    if (isHead != null && ((bool)isHead))
                    {
                        var oldContent = await response.Content.ReadAsByteArrayAsync();
                        var content = new StringContent(string.Empty);
                        content.Headers.Clear();

                        foreach (var header in response.Content.Headers)
                        {
                            content.Headers.Add(header.Key, header.Value);
                        }

                        content.Headers.ContentLength = oldContent.Length;
                        response.Content = content;
                    }
                }
            }
            catch
            {
                //swallow exception here.
            }

            return response;
        }
    }
}
