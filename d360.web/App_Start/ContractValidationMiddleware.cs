using d360.core;
using d360.core.entities;
using d360.extensions.caching;
using d360.web.caching;
using Dapper;
using Microsoft.Owin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace d360.web
{
    public class ContractValidationMiddleware : BaseMiddleware
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

            ContractValidationCacheModel.User resource;
            var resources = Cache.GetItem<ConcurrentBag<ContractValidationCacheModel.User>>(key);

            if (resources == null)
            {
                resources = new ConcurrentBag<ContractValidationCacheModel.User>();
                resource = new ContractValidationCacheModel.User();
                resource.Companies = new ConcurrentBag<ContractValidationCacheModel.Company>();
                resource.ID = resourceId;
                resources.Add(resource);
                Cache.SetItem(key, resources, true, time);
            }
            else
            {
                resource = resources.FirstOrDefault(r => r.ID == resourceId);
            }

            if (resource == null)
            {
                resource = new ContractValidationCacheModel.User();
                resource.ID = resourceId;
                resource.Companies = new ConcurrentBag<ContractValidationCacheModel.Company>();
                Cache.SetItem(key, resources, true, time);
            }
            else
            {
                if (resource.Companies == null)
                    resource.Companies = new ConcurrentBag<ContractValidationCacheModel.Company>();

                var comp = new ConcurrentBag<ContractValidationCacheModel.Company>(resource.Companies ?? new ConcurrentBag<ContractValidationCacheModel.Company>());
                var res = comp.FirstOrDefault(c => c.ID == companyId);
                if (res != null)
                {
                    Cache.SetItem(key, resources, true, time);
                    return res.ContractsAccepted;
                }
            }

            if (resource.Companies == null)
                resource.Companies = new ConcurrentBag<ContractValidationCacheModel.Company>();

            var companies = new ConcurrentBag<ContractValidationCacheModel.Company>(resource.Companies ?? new ConcurrentBag<ContractValidationCacheModel.Company>());
            var resourceCompany = companies.FirstOrDefault(c => c.ID == companyId);

            if (resourceCompany == null)
            {
                resourceCompany = new ContractValidationCacheModel.Company();
                resourceCompany.ID = companyId;

                int contractCount = 0;
                var cnn = await GetCompanyConnection(companyId);

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
                    catch (Exception )
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
                    Cache.SetItem(key, resources, true, time);

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

        public async Task<SqlConnection> GetCompanyConnection(int companyId)
        {
            var str = Cache.GetItemInListByID<string, int>("Company_ConnectionStrings", companyId);
            if (str == null)
            {
                using (var comm = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                {
                    try
                    {
                        comm.Open();
                        
                        var res = await (comm.QuerySingleAsync(@"select s.Server, s.Username, s.Password from Company c
                                inner join DatabaseServer s on s.ID = c.DatabaseServerID 
                                where c.ID = @companyId", new { companyId }));
                                                
                        return new SqlConnection(CompanyConnectionStringHelper.ConnectionString(companyId, res.Server, res.Username, res.Password));
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
            int companyId = 0;
            int resourceId = 0;

            try
            {
                resourceId = context.Get<int>("ResourceID");
                companyId = context.Get<int>("CompanyID");

                if (context.Request.User.Identity.IsAuthenticated)
                {

                    var contractsValidated = await getValidationStatus(companyId, resourceId);
                    context.Set<bool>("ContractsValidated", contractsValidated);
                }
                
            }
            catch(Exception e)
            {
                //log error                
                var telemetry = new Microsoft.ApplicationInsights.TelemetryClient();                
                var properties = new Dictionary<string, string>
                {
                    {"Middleware","ContractValidationMiddleware" },
                    {"CompanyID", companyId.ToString() },
                    {"ResourceID", resourceId.ToString() }
                };
                telemetry.TrackException(e, properties);
                                
                // set validated as true since we cant figure
                context.Set<bool>("ContractsValidated", true);
            }

            await _next.Invoke(environment);
        }
    }
}