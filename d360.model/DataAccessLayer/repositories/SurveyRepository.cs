using d360.core.entities;
using d360.core.entities.SurveyModels;
using d360.core.enums;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class SurveyRepository : ISurveyRepository
    {
        ICompanyContext companyContext;
        public SurveyRepository(ICompanyContext context)
        {
            this.companyContext = context;
        }

        public SurveyType GetSurveyTypeByUid(Guid uid)
        {
            return companyContext.SurveyTypes.FirstOrDefault(x => x.Uid == uid);
        }

        public SurveyApiResponseModel GetSurveysResult(Guid surveyUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var response = new SurveyApiResponseModel();
            response.asOfDate = DateTime.Now.Date;
            response.pageSize = 200;
            response.pageNum = 1;
            response.total = 0;


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


            var pagingSql = $"OFFSET {response.pageSize * (response.pageNum - 1)} ROWS FETCH NEXT {response.pageSize} ROWS ONLY";

            var countQuery = $@"select count(*)
                                    from dbo.SurveyType ST
                                    	inner join Survey S on S.SurveyTypeID = ST.ID
                        	            inner join Asset A on A.Object = s.Object and A.ObjectID = S.ObjectID
                                    where ST.Uid = @surveyTypeUID
                                    {additionalWhereClause}
                                     ";

            response.total = companyContext.Query<int>(countQuery, new { surveyTypeUID = surveyUid }).FirstOrDefault();

            var query = $@"select S.Uid as Uid,
                        	a.uid as AssetUid,
                        	U.uid as UserUid,
                        	S.CreatedOn,
                        	(select 
                        			Q.Uid,
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
                        where ST.Uid = @surveyTypeUID
                        {additionalWhereClause}
                        order by S.CreatedOn
                        {pagingSql}
                        for json path";

            var itemsJson = string.Join("", companyContext.Query<string>(query, new { surveyTypeUID = surveyUid }).ToList());

            response.items = JsonConvert.DeserializeObject<List<SurveyApiModel>>(itemsJson) ?? new List<SurveyApiModel>();
            return response;
        }

        public SurveyTypeApiResponseModel GetSurveyTypes(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var response = new SurveyTypeApiResponseModel();
            response.pageSize = 200;
            response.pageNum = 1;
            response.total = 0;


            var additionalWhereClause = "";
            string orderByClause = "order by ST.CreatedOn";
            foreach (var param in queryParams)
            {
                switch (param.Key.ToLower())
                {
                    case "hasresponses":
                        
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
                    case "_order":
                        switch (param.Value.ToLower())
                        {
                            case "name": orderByClause = "order by ST.Name"; break;
                            case "validfordays": orderByClause = "order by ST.ValidForDays"; break;
                            case "createdon": orderByClause = "order by ST.CreatedOn"; break;
                            case "updatedon": orderByClause = "order by ST.UpdatedOn"; break;
                            case "numberofresponses": orderByClause = "order by NumberOfResponses desc"; break;
                            default: orderByClause = "order by ST.CreatedOn"; break;
                        }
                        break;
                }
            }


            var pagingSql = $"OFFSET {response.pageSize * (response.pageNum - 1)} ROWS FETCH NEXT {response.pageSize} ROWS ONLY";

            var countQuery = $@"select count(*) from dbo.SurveyType ST {additionalWhereClause}";
            response.total = companyContext.Query<int>(countQuery).FirstOrDefault();

            string query = $@";WITH QuestionTypes
                                     AS (select 
		                                QT.Id as TypeId,
		                                QT.Uid,
		                                QT.Name,
		                                QT.Description,
		                                CASE
			                                WHEN QT.DisplayStyle = 1 THEN 'Radio'
			                                WHEN QT.DisplayStyle = 2 THEN 'Rating'
			                                WHEN QT.DisplayStyle = 3 THEN 'CheckList'
		                                END AS DisplayValue,
		                                (select Name, Value from QuestionTypeOption WHERE QuestionTypeID = QT.Id for json path) as Options
		                                from QuestionType QT)
                                select 
	                                ST.Uid,
	                                AT.Uid as AssetTypeUid,
	                                ST.Name,
	                                ST.Description,
	                                ST.ValidForDays,
	                                ST.CreatedOn,
	                                ST.CreatedBy as CreatedByUid,
	                                ST.UpdatedOn,
	                                ST.UpdatedBy as UpdatedByUid,
	                                (select count(*) from survey where SurveyTypeID = ST.ID) as NumberOfResponses,
	                                (select Uid, Name, Description, DisplayValue, Options from QuestionTypes where TypeId = ST.Id for json path) as Questions
                                 from SurveyType ST
                                 inner join AssetType AT on AT.Object =ST.Object AND AT.ObjectID = ST.ObjectID 
                                {additionalWhereClause}
                                {orderByClause}
                                {pagingSql}
                                for json path";

            var itemsJson = string.Join("", companyContext.Query<string>(query).ToList());

            response.items = JsonConvert.DeserializeObject<List<SurveyTypeApiModel>>(itemsJson) ?? new List<SurveyTypeApiModel>();
            return response;
        }

    }
}