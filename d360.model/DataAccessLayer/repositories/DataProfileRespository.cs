using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core;
using d360.model.DataAccessLayer.repositories;

namespace d360.model.DataAccessLayer
{
    public class DataProfileRepository : BaseRepository, IDataProfileRepository
    {
        internal ICompanyContext CompanyContext;
        internal ICommunityContext Community;

        public DataProfileRepository(ICompanyContext companyContext, ICommunityContext community)
            : base(companyContext)
        {
            this.CompanyContext = companyContext;
            this.Community = community;
        }

        public Task<List<DataProfileUpsertResponse>> PostDataProfiles(DataProfileModel model)
        {
            throw new NotImplementedException();
        }

        public List<DataProfileUpsertResponse> UpsertDataProfiles(List<DataProfileUpsertModel> DataProfileUpsertModels, ApiExecution execution, bool isInsert)
        {
            CompanyContext.Add(execution);

            List<DataProfileUpsertResponse> results = null;
            
            try
            {
                results = CompanyContext.UpsertDataProfiles(DataProfileUpsertModels, execution, isInsert);

                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }            

            return results;
        }
    }
}
