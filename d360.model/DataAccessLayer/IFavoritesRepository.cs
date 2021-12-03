using d360.core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IFavoritesRepository
    {
        Task<IReadOnlyList<BreadcrumbItemResponse>> GetBreadcrumbs(IEnumerable<BreadcrumbForObjectRequest> items);
    }

    public class BreadcrumbForObjectRequest
    {
        public SystemObjects ObjectType { get; set; }

        public int ObjectId { get; set; }
    }

    public class BreadcrumbItemResponse
    {
        public SystemObjects ForObjectType { get; set; }

        public int ForObjectId { get; set; }

        public int Level { get; set; }

        public string TypeName { get; set; }

        public string Name { get; set; }

        public string TypeUrl { get; set; }

        public string Url { get; set; }
    }
}