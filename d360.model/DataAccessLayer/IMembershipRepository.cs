using d360.core.entities;
using d360.core.entities.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IMembershipRepository
    {
        Task<GroupApiModels> GetGroups(IEnumerable<KeyValuePair<string, string>> queryParams);
        WorkHttpStatus DeleteResources(IEnumerable<UserApiDeleteModel> resources);
        Task<IEnumerable<UserApiUpsertResult>> UpsertUsers(ApiExecution execution, IEnumerable<IUserApiUpsertModel> users);
    }
}
