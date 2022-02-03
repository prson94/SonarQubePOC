using System;
using System.Linq;
using System.Net.Http;
using d360.core.types;
using Resources;

namespace d360.web.Utilities
{
    public class RequestValidator: IRequestValidator
    {
        private ITypeServiceProvider TypeServiceProvider { get; }
        private const int DefaultPageSize = 200;
        private const int PageSizeLimitJson = 250;
        private const int PageSizeLimitStream = 200000;
        private const int PageIndexLimit = 10000;

        public RequestValidator(ITypeServiceProvider typeServiceProvider)
        {
            TypeServiceProvider = typeServiceProvider;
        }

        public int ValidateJsonPageSize(string pageSize)
        {
            return ValidatePageSize(pageSize, DefaultPageSize, PageSizeLimitJson);
        }

        public int ValidateStreamPageSize(string pageSize)
        {
            return ValidatePageSize(pageSize, DefaultPageSize, PageSizeLimitStream);
        }

        public int ValidateJsonPageSize(int? pageSize)
        {
            return ValidatePageSize(pageSize, DefaultPageSize, PageSizeLimitJson);
        }

        public int ValidateStreamPageSize(int? pageSize)
        {
            return ValidatePageSize(pageSize, DefaultPageSize, PageSizeLimitStream);
        }

        public int ValidatePageSize(string pageSize, int defaultPageSize, int pageSizeLimit)
        {
            int result = defaultPageSize;

            if (!string.IsNullOrEmpty(pageSize))
            {
                if (TypeServiceProvider.Int32.TryParse(pageSize, out result))
                {
                    return ValidatePageSize(result, defaultPageSize, pageSizeLimit);
                }
                else
                {
                    throw new ArgumentException(ApiMessages.NumberValueMessage, nameof(pageSize));
                }
            }

            return result;
        }

        public int ValidatePageSize(int? pageSize, int defaultPageSize, int pageSizeLimit)
        {
            int result = defaultPageSize;

            if (pageSize != null)
            {
                if (pageSize > pageSizeLimit)
                {
                    throw new ArgumentException(ApiMessages.InvalidNumberTooLarge, nameof(pageSize));
                }

                if (pageSize <= 0)
                {
                    throw new ArgumentException(ApiMessages.MinLengthCheckGTZero, nameof(pageSize));
                }

                result = pageSize.Value;
            }

            return result;
        }

        public int ValidatePageNumber(string pageNumber)
        {
            return ValidatePageNumber(pageNumber, PageIndexLimit);
        }

        public int ValidatePageNumber(int? pageNumber)
        {
            return ValidatePageNumber(pageNumber, PageIndexLimit);
        }

        public int ValidatePageNumber(string pageNumber, int pageIndexLimit)
        {
            int result = 1;

            if (!string.IsNullOrEmpty(pageNumber))
            {
                if (TypeServiceProvider.Int32.TryParse(pageNumber, out result))
                {
                    return ValidatePageNumber(result, pageIndexLimit);
                }
                else
                {
                    throw new ArgumentException(ApiMessages.NumberValueMessage, nameof(pageNumber));
                }
            }

            return result;
        }

        public int ValidatePageNumber(int? pageNumber, int pageIndexLimit)
        {
            int result = 1;

            if (pageNumber != null)
            {
                if (pageNumber > pageIndexLimit)
                {
                    throw new ArgumentException(ApiMessages.InvalidNumberTooLarge, nameof(pageNumber));
                }

                if (pageNumber <= 0)
                {
                    throw new ArgumentException(ApiMessages.MinLengthCheckGTZero, nameof(pageNumber));
                }

                result = pageNumber.Value;
            }

            return result;
        }

        public bool IsStreamResponse(HttpRequestMessage request)
        {
            return request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
        }
    }
}