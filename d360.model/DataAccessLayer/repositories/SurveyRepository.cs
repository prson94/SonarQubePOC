using d360.core.entities;
using d360.core.entities.SurveyModels;
using d360.core.enums;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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


            var additionalWhereClause = string.Empty;
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
                        response.asOfDate = date;
                        additionalWhereClause += $" AND S.CreatedOn <= '{date.AddDays(1)}'";
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
                        			QT.Uid,
                        			Q.Comment, 
                        			(select QTO.Name, QTO.Value from QuestionTypeOption QTO 
                        				inner join QuestionOption QO ON Q.ID = QO.QuestionID
										inner join QuestionType QT ON QTO.QuestionTypeID = QT.ID
                        				where QO.QuestionTypeOptionID = QTO.id 
                        				for json path) as Response		
                        		from Question Q
									inner join QuestionOption QO on QO.QuestionID = Q.ID
									inner join QuestionTypeOption QTO on QTO.ID = QO.QuestionTypeOptionID
									inner join QuestionType QT on QT.ID = QTO.QuestionTypeID
                        	    where Q.SurveyID = S.Id for json path) as Questions
                        
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


            string orderByClause = "order by ST.CreatedOn";
            List<string> whereClauses = new List<string>();
            foreach (var param in queryParams)
            {
                switch (param.Key.ToLower())
                {
                    case "hasresponses":
                        if (bool.Parse(param.Value))
                        {
                            whereClauses.Add("Responses > 0");
                        }
                        else
                        {
                            whereClauses.Add("Responses is null or Responses = 0");
                        }
                        break;
                    case "assettypeuid":
                        Guid uid = Guid.Parse(param.Value);
                        whereClauses.Add($"AT.Uid = '{uid}'");
                        break;
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
                            case "validfordays": orderByClause = "order by ST.ValidForDays desc"; break;
                            case "createdon": orderByClause = "order by ST.CreatedOn"; break;
                            case "updatedon": orderByClause = "order by ST.UpdatedOn"; break;
                            case "numberofresponses": orderByClause = "order by NumberOfResponses desc"; break;
                            default: throw new Exception("Invalid value for order parameter. Use Name|ValidForDays|CreatedOn|UpdatedOn|NumberOfResponses!");
                        }
                        break;
                }
            }

            var additionalWhereClause = whereClauses.Count > 0 ? $"where {string.Join(" AND ", whereClauses)}" : "";

            var pagingSql = $"OFFSET {response.pageSize * (response.pageNum - 1)} ROWS FETCH NEXT {response.pageSize} ROWS ONLY";

            var countQuery = $@"select  count(*) from dbo.SurveyType ST 
                                            inner join AssetType AT on AT.Object =ST.Object AND AT.ObjectID = ST.ObjectID 
                                            left join (select SurveyTypeId, Count(*) as Responses from Survey Group by SurveyTypeId)Responses 
                                                on Responses.SurveyTypeId = ST.Id {additionalWhereClause}";
            response.total = companyContext.Query<int>(countQuery).FirstOrDefault();

            string QuestionsCTE = @"select 
		                                ST.Id as TypeId,
		                                QT.Uid,
		                                QT.Name,
		                                QT.Description,
		                                CASE
			                                WHEN QT.DisplayStyle = 1 THEN 'Radio'
			                                WHEN QT.DisplayStyle = 2 THEN 'Rating'
			                                WHEN QT.DisplayStyle = 3 THEN 'CheckList'
		                                END AS DisplayStyle,
		                                (select Name, Value from QuestionTypeOption WHERE QuestionTypeID = QT.Id for json path) as Options
		                                from QuestionType QT
                                        cross apply(
											select ST.ID From SurveyType ST where QT.SurveyTypeID = ST.ID
											)ST(Id)";

            string query = $@";WITH QuestionTypes AS ({QuestionsCTE})
                                select 
	                                ST.Uid,
	                                AT.Uid as AssetTypeUid,
	                                ST.Name,
	                                ISNULL(ST.Description, '') as Description,
	                                ST.ValidForDays,
	                                ST.CreatedOn,
	                                ACreate.uid as CreatedByUid,
	                                ST.UpdatedOn,
	                                AUpdate.uid as UpdatedByUid,
	                                Responses as NumberOfResponses,
	                                (select Uid, Name, Description, DisplayStyle, Options from QuestionTypes where TypeId = ST.Id for json path) as Questions
                                 from SurveyType ST
                                 inner join AssetType AT on AT.Object = ST.Object AND AT.ObjectID = ST.ObjectID 
                                 inner join Asset ACreate on ACreate.Object = 'Resource' AND ACreate.ObjectID = ST.CreatedBy
                                 inner join Asset AUpdate on AUpdate.Object = 'Resource' AND AUpdate.ObjectID = ST.UpdatedBy
								 left join (select SurveyTypeId, Count(*) as Responses from Survey Group by SurveyTypeId)Responses on Responses.SurveyTypeId = ST.Id
                                {additionalWhereClause}
                                {orderByClause}
                                {pagingSql}
                                for json path";

            var itemsJson = string.Join("", companyContext.Query<string>(query).ToList());

            response.items = JsonConvert.DeserializeObject<List<SurveyTypeApiModel>>(itemsJson) ?? new List<SurveyTypeApiModel>();
            return response;
        }

        public SurveyResultSummaryApiResponseModel GetSurveyResultSummary(Guid surveyTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var response = new SurveyResultSummaryApiResponseModel();
            response.pageSize = 200;
            response.pageNum = 1;
            response.total = 0;
            response.asOfDate = DateTime.Now.Date;


            string orderByClause = "order by First.CreatedOn";
            var additionalWhereClause = string.Empty;

            List<string> whereClauses = new List<string>();
            foreach (var param in queryParams)
            {
                switch (param.Key.ToLower())
                {
                    case "assetuid":
                        Guid assetGuid = Guid.Parse(param.Value);
                        whereClauses.Add($"A.uid = '{assetGuid}'");
                        break;
                    case "asofdate":
                        DateTime date = DateTime.MinValue;
                        if (!DateTime.TryParse(param.Value, out date))
                        {
                            throw new Exception("Invalid date value for AsOfDate parameter!");
                        }

                        whereClauses.Add($"S.CreatedOn <= '{date.Date.AddDays(1)}'");
                        response.asOfDate = date;
                        break;
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
                            case "firstrespondedon": orderByClause = "order by First.CreatedOn"; break;
                            case "lastrespondedon": orderByClause = "order by Last.CreatedOn"; break;
                            case "numberofresponders": orderByClause = "order by QD.Responders desc"; break;
                            default: throw new Exception("Invalid value for order parametar! Use FirstRespondedOn|LastRespondedOn|NumberOfResponders");
                        }
                        break;
                }
            }


            var pagingSql = $"OFFSET {response.pageSize * (response.pageNum - 1)} ROWS FETCH NEXT {response.pageSize} ROWS ONLY";

            var countWhereClause = whereClauses.Count > 0 ? "and " + string.Join(" and ", whereClauses) : ""; 
            var countQuery = $@"select count(distinct A.uid) from Asset A
                                inner join Survey S ON A.Object = S.Object and A.ObjectID = S.ObjectID
                                inner join SurveyType ST on S.SurveyTypeID = ST.ID
                                where ST.uid = @surveyTypeUid
                                {countWhereClause}";
            response.total = companyContext.Query<int>(countQuery, new { surveyTypeUid }).FirstOrDefault();

            if (whereClauses.Count > 0)
                additionalWhereClause = "WHERE " + string.Join(" and ", whereClauses);

            string AnswersCTE = $@"
	                        select S.uid as SurveyUid,S.CreatedOn, A.uid as AssetUid, QT.Uid as QuestionTypeID, QTO.Name,QTO.Value from QuestionTypeOption QTO
	                        		inner join QuestionOption QO on QO.QuestionTypeOptionID = QTO.ID
	                        		inner join Question Q on QO.QuestionID = Q.ID
	                        		inner join QuestionType QT on QT.ID = QTO.QuestionTypeID
	                        		inner join Survey S on Q.SurveyID = S.ID
	                        		inner join Asset A on A.Object = S.Object and A.ObjectID = S.ObjectID
                                    {additionalWhereClause}";

            string QuestionsCTE = @"select 
	                        	 	QT.Uid, 
	                        		S.SurveyTypeID, 
	                        		A.Uid as AssetUid,
	                        		(select AD.Name, AD.Value, count(*) as Count 
	                        		   from AnswerData AD 
	                        		   where AD.AssetUid = A.Uid AND AD.QuestionTypeID = QT.Uid 
	                        		   group by AD.Name, AD.Value
	                        		   for json path) Responses
	                        	 from QuestionOption QO
	                        		inner join QuestionTypeOption QTO on QTO.ID = QO.QuestionTypeOptionID
	                        		inner join Question Q on QO.QuestionId = Q.Id
	                        		inner join Survey S on S.ID = Q.SurveyID
	                        		inner join Asset A ON S.Object = A.Object AND S.ObjectID = A.ObjectID
	                        		inner join QuestionType QT on QT.ID = QTO.QuestionTypeID
	                        		group by QT.Uid, S.SurveyTypeID, A.Uid";

            var sql = $@";with AnswerData as ({AnswersCTE}),QuestionsData as ({QuestionsCTE})
	                        select  
	                        A.uid as AssetUid,
	                        First.CreatedOn as FirstRespondedOn,
	                        Last.CreatedOn as LastRespondedOn,
	                        QD.Responders as NumberOfResponders,
	                        (select Uid, Responses
	                        	from QuestionsData 
	                        	where AssetUid = A.uid 
	                        	for json path) AS Questions 
	                        from SurveyType ST 
	                        inner join Survey S on S.SurveyTypeID = ST.ID
	                        inner join Asset A ON S.Object = A.Object AND S.ObjectID = A.ObjectID
	                        cross apply (select count(distinct SurveyUid) as Responders from AnswerData where AssetUid = A.uid)QD
	                        cross apply (select top 1 CreatedOn from AnswerData where AssetUid = A.uid order by CreatedOn)First
	                        cross apply (select top 1 CreatedOn from AnswerData where AssetUid = A.uid order by CreatedOn desc)Last
	                        where ST.Uid = @surveyTypeUid
	                        group by A.uid, QD.Responders, First.CreatedOn, Last.CreatedOn
                            {orderByClause}
                            {pagingSql}
	                        for json path";

            var itemsJson = string.Join("", companyContext.Query<string>(sql, new { surveyTypeUid }).ToList());

            response.items = JsonConvert.DeserializeObject<List<SurveyResultSummaryApiModel>>(itemsJson) ?? new List<SurveyResultSummaryApiModel>();
            return response;
        }


    }
}