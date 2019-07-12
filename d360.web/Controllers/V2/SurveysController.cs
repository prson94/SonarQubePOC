using d360.core.entities;
using d360.core.enums;
using d360.model;
using d360.model.DataAccessLayer;
using d360.model.validators;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using d360.core.entities.SurveyModels;
using Newtonsoft.Json;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling tag management in Govern
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/survey"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = false)
    ]
    public class SurveysController : BaseV2ApiController
    {

        public SurveysController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        {
        }

        /// <summary>
        /// Returns all survey results defined in Govern.          
        /// </summary>        
        /// <returns>A list of survey results</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("{surveyTypeUid:int}/results"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("AssetUid", "The uid of a specific asset to return.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AsOfDate", "Pull results up to a certain date.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of tags.", typeof(SurveyApiResponseModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<HttpResponseMessage> GetSurveysResultsAsync(int surveyTypeUid)
        {
            var prefix = "Surveys.GetSurveysResultsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                var response = new SurveyApiResponseModel();
                response.asOfDate = DateTime.Now.Date;
                response.pageSize = 200;
                response.pageNum = 1;
                response.total = 0;

                var queryParams = Request.GetQueryNameValuePairs();

                var additionalWhereClause = "";
                foreach (var param in queryParams)
                {
                    switch (param.Key.ToLower())
                    {
                        case "_pagesize":
                            int size = 0;
                            if (int.TryParse(param.Value, out size))
                            {
                                response.pageSize = int.Parse(param.Value);
                            }
                            else throw new Exception("Invalid value for page size parametar!");
                            break;
                        case "_pagenum":
                            int num = 0;
                            if (int.TryParse(param.Value, out num))
                            {
                                response.pageNum = int.Parse(param.Value);
                                if (response.pageNum <= 0) response.pageNum = 1;
                            }
                            else throw new Exception("Invalid value for page number parametar!");
                            break;
                        case "assetuid":
                            Guid uid = Guid.Parse(param.Value);
                            if (uid == Guid.Empty)
                                throw new Exception("Invalid value for asset uid!");

                            additionalWhereClause += $" AND a.uid = '{uid}'";
                            break;
                        case "asofdate":
                            DateTime date = DateTime.MinValue;
                            if (!DateTime.TryParse(param.Value, out date))
                            {
                                throw new Exception("Invalid date value for AsOfDate parameter!");
                            }
                            response.asOfDate = date.AddDays(1);
                            additionalWhereClause += $" AND S.CreatedOn <= '{response.asOfDate.ToString()}'";
                            break;
                    }
                }

                var countQuery = $@"select count(*)
                                    from dbo.SurveyType ST
                                    	inner join Survey S on S.SurveyTypeID = ST.ID
                        	            inner join Asset A on A.Object = s.Object and A.ObjectID = S.ObjectID
                                    where ST.ID = @surveyTypeUID
                                    {additionalWhereClause}
                                     ";

                var pagingSql = $"OFFSET {response.pageSize * (response.pageNum - 1)} ROWS FETCH NEXT {response.pageSize} ROWS ONLY";


                var query = $@"select S.ID as Uid,
                        	a.uid as AssetUid,
                        	U.uid as UserUid,
                        	S.CreatedOn,
                        	(select 
                        			Q.ID,
                        			Q.Comment, 
                        			(select QTO.Name, QTO.Value from QuestionTypeOption QTO 
                        				inner join QuestionOption QO ON Q.ID = QO.QuestionID
                        				where QO.QuestionTypeOptionID = QTO.id 
                        				for json path) as Response		
                        		from Question Q
                        	    where Q.SurveyID = S.Id for json path) as Question
                        
                         from dbo.SurveyType ST
                        	inner join Survey S on S.SurveyTypeID = ST.ID
                        	inner join Asset A on A.Object = s.Object and A.ObjectID = S.ObjectID
                        	inner join Asset U on U.Object = 'Resource' and U.ObjectID = S.ResourceID
                        where ST.ID = @surveyTypeUID
                        {additionalWhereClause}
                        order by S.CreatedOn
                        {pagingSql}
                        for json path";

                var itemsJson = string.Join("", Company.Query<string>(query, new { surveyTypeUID = surveyTypeUid }).ToList());

                response.items = JsonConvert.DeserializeObject<List<SurveyApiModel>>(itemsJson);
                response.total = Company.Query<int>(countQuery, new { surveyTypeUID = surveyTypeUid }).FirstOrDefault();

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }

            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }

        }


    }
}
