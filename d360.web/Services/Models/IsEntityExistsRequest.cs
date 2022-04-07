using MediatR;

namespace d360.web.Services
{
    internal abstract class IsEntityExistsRequest : IsEntityExistsRequest<IsEntityExistsResponse>
    {

    }

    internal abstract class IsEntityExistsRequest<TResponse> : IRequest<TResponse>
        where TResponse : IsEntityExistsResponse
    {
        protected IsEntityExistsRequest()
        {
            ThrowNotFoundException = false;
        }

        public bool ThrowNotFoundException { get; set; }
    }
}
