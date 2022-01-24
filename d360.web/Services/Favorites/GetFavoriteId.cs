using System.Threading;
using System.Threading.Tasks;
using d360.model.DataAccessLayer;
using MediatR;

namespace d360.web.Services
{
    // This query exists, because we can't return anything (even Ids) from commands
    // Typically, we will generate ids before commands, but we can't do that for ToggleFavoriteOrHomePageCommand 
    // Because it's a toggle command, not simple create command
    public class GetFavoriteId : IRequestHandler<GetFavoriteId.Argument, int?>
    {
        private readonly IFavoritesRepository favoritesRepository;

        public GetFavoriteId(IFavoritesRepository favoritesRepository)
        {
            this.favoritesRepository = favoritesRepository;
        }

        public async Task<int?> Handle(Argument request, CancellationToken cancellationToken)
        {
            request.Route = request.Route.Trim();
            return await favoritesRepository.GetFavoriteIdByRoute(request.ResourceId, request.Route);
        }

        public class Argument : IRequest<int?>
        {
            public int ResourceId { get; set; }

            public string Route { get; set; }
        }
    }
}