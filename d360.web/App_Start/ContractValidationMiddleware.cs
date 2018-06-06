using d360.core;
using d360.core.entities;
using d360.extensions.caching;
using Dapper;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace d360.web
{
    public class ContractValidationMiddleware
    {

        Func<IDictionary<string, object>, Task> _next;
        public ContractValidationMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        async Task<bool> getValidationStatus(int companyId, int resourceId)
        {
            var key = ContractValidationCacheModel.cacheKey;
            var time = ContractValidationCacheModel.cacheDuration;
            var cache = new MemoryCachingProvider();

            var resource = cache.GetItemInListByID<ContractValidationCacheModel.User, int>(key, resourceId);

            if (resource == null)
            {
                resource = new ContractValidationCacheModel.User();
                resource.Companies = new List<ContractValidationCacheModel.Company>();
                cache.SetItemInListByID(key, resourceId, resource, true, time);
            }
            else
            {
                if (resource.Companies == null)
                    resource.Companies = new List<ContractValidationCacheModel.Company>();

                var comp = new List<ContractValidationCacheModel.Company>(resource.Companies ?? new List<ContractValidationCacheModel.Company>());
                var res = comp.FirstOrDefault(c => c.ID == companyId);
                if (res != null)
                {
                    cache.SetItemInListByID(key, resourceId, resource, true, time);
                    return res.ContractsAccepted;
                }
            }

            if (resource.Companies == null)
                resource.Companies = new List<ContractValidationCacheModel.Company>();

            var companies = new List<ContractValidationCacheModel.Company>(resource.Companies ?? new List<ContractValidationCacheModel.Company>());
            var resourceCompany = companies.FirstOrDefault(c => c.ID == companyId);

            if (resourceCompany == null)
            {
                resourceCompany = new ContractValidationCacheModel.Company();
                resourceCompany.ID = companyId;

                int contractCount = 0;
                var cnn = GetCompanyConnection(companyId);

                if (cnn != null)
                {

                    try
                    {
                        cnn.Open();
                        var result = await cnn.QueryAsync<int>(@"select count(*) from dbo.GetContractValidations(@ResourceID) where accepted = 0 and ((contractType = 1 and isFirstUser = 1) or contractType = 2 or organizationId is null)", new { resourceId });
                        if (result != null)
                            contractCount = result.FirstOrDefault();
                        else
                            contractCount = 0;
                    }
                    catch (Exception ex)
                    {
                        contractCount = 0;
                    }
                    finally
                    {
                        cnn.Close();
                        cnn.Dispose();
                    }
                    resourceCompany.ContractsAccepted = contractCount == 0;
                    resource.Companies.Add(resourceCompany);
                    cache.SetItemInListByID(key, resourceId, resource, true, time);

                    return resourceCompany.ContractsAccepted;

                }
                else
                {
                    return true;
                }
            }
            else
            {
                return resourceCompany.ContractsAccepted;
            }
        }

        public SqlConnection GetCompanyConnection(int companyId)
        {
            var cache = new MemoryCachingProvider();

            var str = cache.GetItemInListByID<string, int>("Company_ConnectionStrings", companyId);
            if (str == null)
            {
                using (var comm = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                {
                    try
                    {
                        var res = comm.Query<string>(@"select 'server=' + s.Server + ';Database=D3S_' + cast(@companyId as varchar) + ';User ID=' + s.Username + ';Password='+ s.Password + ';MultipleActiveResultSets=True;' from Company c
                                inner join DatabaseServer s on s.ID = c.DatabaseServerID 
                                where c.ID = @companyId", new { companyId }).FirstOrDefault();
                        return new SqlConnection(res);
                    }
                    catch
                    {
                        return null;
                    }

                }
            }
            else
                return new SqlConnection(str);
        }


        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);

            var resourceId = context.Get<int>("ResourceID");
            var companyId = context.Get<int>("CompanyID");

            if (context.Request.User.Identity.IsAuthenticated)
            {

                var contractsValidated = await getValidationStatus(companyId, resourceId);
                context.Set<bool>("ContractsValidated", contractsValidated);
            }



            await _next.Invoke(environment);
        }
    }
}