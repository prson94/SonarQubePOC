using System.Net.Http;

namespace d360.web.Utilities
{
    public interface IRequestValidator
    {
        bool IsStreamResponse(HttpRequestMessage request);

        int ValidateJsonPageSize(string pageSize);

        int ValidateJsonPageSize(int? pageSize);

        int ValidateStreamPageSize(string pageSize);

        int ValidateStreamPageSize(int? pageSize);

        int ValidatePageSize(string pageSize, int defaultPageSize, int pageSizeLimit);

        int ValidatePageSize(int? pageSize, int defaultPageSize, int pageSizeLimit);

        int ValidatePageNumber(string pageNumber);

        int ValidatePageNumber(string pageNumber, int pageIndexLimit);

        int ValidatePageNumber(int? pageNumber);

        int ValidatePageNumber(int? pageNumber, int pageIndexLimit);
    }
}