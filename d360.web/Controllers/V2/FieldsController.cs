using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/fields"),
        Authorize,
        StringEnumController
    ]
    public class FieldsController : BaseV2ApiController
    {
        #region DI

        IQueueSource QueueSource;
        IStorageProvider Storage;

        public FieldsController(ICommunityContext community, ICompanyContext company, IStorageProvider storage, IQueueSource queueSource)
            : base(community, company)
        {
            QueueSource = queueSource;
            Storage = storage;
        }

        #endregion

        private async Task<FieldTypesApiViewModel> GetFieldTypes(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            Guid? actionTypeUid = null;
            Guid? assetTypeUid = null;
            Guid? relationshipTypeUid = null;
            int pageNumber = 1;
            int pageSize = 250;

            var whereClause = "";

            #region Parameter Checking

            var dbArgs = new DynamicParameters();

            var parameters = queryParams.ToList();

            string obj = null;
            int? objID = null;

            if (parameters.Any(q => q.Key.ToLower() == "actiontypeuid"))
            {
                var actionTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "actiontypeuid").Value;
                Guid ac;
                if (Guid.TryParse(actionTypeUidString, out ac))
                {
                    actionTypeUid = ac;
                    var actionType = Company.Filter<IssueType>(i => i.uid == actionTypeUid).SingleOrDefault();
                    if (actionType != null)
                    {
                        obj = "IssueType";
                        objID = actionType.ID;
                    }
                    else
                    {
                        throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Action Type not found based on Uid provided [{actionTypeUid.ToString()}].");
                    }
                }
            }
            if (parameters.Any(q => q.Key.ToLower() == "assettypeuid"))
            {
                if (actionTypeUid.HasValue)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an AssetTypeUid since you have already provided an ActionTypeUid.");
                }
                else
                {
                    var assetTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value;
                    Guid at;
                    if (Guid.TryParse(assetTypeUidString, out at))
                    {
                        assetTypeUid = at;
                        var assetType = Company.Filter<AssetType>(i => i.uid == assetTypeUid).SingleOrDefault();
                        if (assetType != null)
                        {
                            obj = assetType.Object;
                            objID = assetType.ObjectID;
                        }
                        else
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Asset Type not found based on Uid provided [{assetTypeUid.ToString()}].");
                        }
                    }
                }
            }
            if (parameters.Any(q => q.Key.ToLower() == "relationshiptypeuid"))
            {
                if (actionTypeUid.HasValue)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an ActionTypeUid.");
                }
                else if (assetTypeUid.HasValue)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an AssetTypeUid.");
                }
                else
                {
                    var relationshipTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "relationshiptypeuid").Value;
                    Guid rt;
                    if (Guid.TryParse(relationshipTypeUidString, out rt))
                    {
                        relationshipTypeUid = rt;
                        var intersectType = Company.Filter<IntersectType>(i => i.uid == relationshipTypeUid).SingleOrDefault();
                        if (intersectType != null)
                        {
                            obj = "IntersectType";
                            objID = intersectType.ID;
                        }
                        else
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Relationship Type not found based on Uid provided [{relationshipTypeUid.ToString()}].");
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(obj) && objID.HasValue)
            {
                dbArgs.Add("@obj", obj);
                dbArgs.Add("@objID", objID.Value);
                whereClause += (string.IsNullOrEmpty(whereClause) ? " where " : " and ") + $"FT.[Object] = @obj and FT.[ObjectID] = @objID";
            }

            if (parameters.Any(q => q.Key.ToLower() == "name"))
            {
                var fieldTypeName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "name").Value.ToLower();
                dbArgs.Add("@name", fieldTypeName);
                whereClause += (string.IsNullOrEmpty(whereClause) ? " where " : " and ") + $"lower(FT.[Name]) = @name";
            }

            if (parameters.Any(q => q.Key.ToLower() == "friendlyname"))
            {
                var fieldTypeFriendlyName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "friendlyname").Value.ToLower();
                dbArgs.Add("@fname", fieldTypeFriendlyName);
                whereClause += (string.IsNullOrEmpty(whereClause) ? " where " : " and ") + $"lower(FT.[FriendlyName]) = @fname";
            }

            if (parameters.Any(q => q.Key.ToLower() == "type"))
            {
                var fieldTypeType = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "type").Value.ToLower();
                dbArgs.Add("@type", fieldTypeType);
                whereClause += (string.IsNullOrEmpty(whereClause) ? " where " : " and ") + $"lower(FT.[Type]) = @type";
            }

            if (parameters.Any(q => q.Key.ToLower() == "_pagenum"))
            {
                var pageNumberString = parameters.FirstOrDefault(q => q.Key.ToLower() == "_pagenum").Value;
                if (!int.TryParse(pageNumberString, out pageNumber))
                {
                    pageNumber = 1;
                }
            }
            if (parameters.Any(q => q.Key.ToLower() == "_pagesize"))
            {
                var pageSizeString = parameters.FirstOrDefault(q => q.Key.ToLower() == "_pagesize").Value;
                if (!int.TryParse(pageSizeString, out pageSize))
                {
                    pageSize = 250;
                }
            }

            if (pageNumber < 0)
            {
                pageNumber = 1;
            }
            if (pageSize < 0 || pageSize > 250)
            {
                pageSize = 250;
            }

            dbArgs.Add("@pageNum", pageNumber);
            dbArgs.Add("@pageSize", pageSize);

            #endregion
           
            var sql = $@"
declare @total int
select	@total = count(1) from FieldType FT {whereClause}

select	@pageSize as 'pageSize',
		@pageNum as 'pageNum',
		@total as 'total',
		(
        select	
		        FT.Name,
		        FT.FriendlyName,
		        FT.Category,
				IIF(FT.Object = 'IssueType', O_I.Uid , null) as ActionTypeUid,
				IIF(FT.Object <> 'IssueType' AND FT.Object <> 'IntersectType', O_A.Uid , null) as AssetTypeUid,
				IIF(FT.Object = 'IntersectType', O_R.Uid , null) as RelationshipTypeUid,

                case when FT.Type = 'Boolean' then FT.ColumnOrder else null end as 'Type.Boolean.ColumnOrder',
		        case when FT.Type = 'Boolean' then FT.ColumnWidth else null end as 'Type.Boolean.ColumnWidth',
		        case when FT.Type = 'Boolean' then FT.SortOrder else null end as 'Type.Boolean.SortOrder',
		        case when FT.Type = 'Boolean' then TRY_CAST(FT.DefaultValue as bit) else null end as 'Type.Boolean.DefaultValue',
		        case when FT.Type = 'Boolean' then FT.DisplayDescription else null end as 'Type.Boolean.Description.Display',
		        case when FT.Type = 'Boolean' then FT.FormDescription else null end as 'Type.Boolean.Description.Form',
		        case when FT.Type = 'Boolean' then FT.IsDisplayable else null end as 'Type.Boolean.IsDisplayable',
		        case when FT.Type = 'Boolean' then FT.IsEditable else null end as 'Type.Boolean.IsEditable',
		        case when FT.Type = 'Boolean' then FT.IsListable else null end as 'Type.Boolean.IsListable',
		        case when FT.Type = 'Boolean' then FT.IsPartOfKey else null end as 'Type.Boolean.IsPartOfKey',
		        case when FT.Type = 'Boolean' then FT.IsPrimaryFilter else null end as 'Type.Boolean.IsPrimaryFilter', 
                case when FT.Type = 'Boolean' then FT.ShowIfEmpty else null end as 'Type.Boolean.ShowIfEmpty', 

		        case when FT.Type = 'FusionLookup' then FT.ColumnOrder else null end as 'Type.ComputedFusionLookup.ColumnOrder',

		        case when FT.Type = 'OwnershipLookup' then FT.ColumnOrder else null end as 'Type.ComputedOwnershipLookup.ColumnOrder',
		        case when FT.Type = 'OwnershipLookup' then FT.DisplayDescription else null end as 'Type.ComputedOwnershipLookup.Description.Display',
		        case when FT.Type = 'OwnershipLookup' then FTL.HideHeader else null end as 'Type.ComputedOwnershipLookup.HideHeader', 
		        case when FT.Type = 'OwnershipLookup' then FTL.HideFooter else null end as 'Type.ComputedOwnershipLookup.HideFooter', 
		        case when FT.Type = 'OwnershipLookup' then FTL.HideFilter else null end as 'Type.ComputedOwnershipLookup.HideFilter', 
                case when FT.Type = 'OwnershipLookup' then try_cast(JSON_VALUE(FTL.Definition, '$.DisplayAssignmentSource') as bit) else null end as 'Type.ComputedOwnershipLookup.Definition.DisplayAssignmentSource',
		        case when FT.Type = 'OwnershipLookup' then try_cast(JSON_VALUE(FTL.Definition, '$.ExpandGroupMembership') as bit) else null end as 'Type.ComputedOwnershipLookup.Definition.ExpandGroupMembership',
		        case when FT.Type = 'OwnershipLookup' then FT.IsDisplayable else null end as 'Type.ComputedOwnershipLookup.IsDisplayable',
		        case when FT.Type = 'OwnershipLookup' then FT.ShowIfEmpty else null end as 'Type.ComputedOwnershipLookup.ShowIfEmpty',

		        case when FT.Type = 'FieldFromRelationship' then FT.ColumnOrder else null end as 'Type.ComputedRelationshipField.ColumnOrder',
		        case when FT.Type = 'FieldFromRelationship' then FT.ColumnWidth else null end as 'Type.ComputedRelationshipField.ColumnWidth',
		        case when FT.Type = 'FieldFromRelationship' then FT.SortOrder else null end as 'Type.ComputedRelationshipField.SortOrder',
		        case when FT.Type = 'FieldFromRelationship' then FT.DisplayDescription else null end as 'Type.ComputedRelationshipField.Description.Display',
		        case when FT.Type = 'FieldFromRelationship' then IT.Uid else null end as 'Type.ComputedRelationshipField.IntersectTypeUid',
		        case when FT.Type = 'FieldFromRelationship' then LFT.Name else null end as 'Type.ComputedRelationshipField.FieldTypeName',
		        case when FT.Type = 'FieldFromRelationship' then FT.IsDisplayable else null end as 'Type.ComputedRelationshipField.IsDisplayable',
		        case when FT.Type = 'FieldFromRelationship' then FT.IsListable else null end as 'Type.ComputedRelationshipField.IsListable',
		        case when FT.Type = 'FieldFromRelationship' then FT.ShowIfEmpty else null end as 'Type.ComputedRelationshipField.ShowIfEmpty',

		        case when FT.Type = 'ComplexRelationLookup' then FT.ColumnOrder else null end as 'Type.ComputedRelationshipLookup.ColumnOrder',
		        case when FT.Type = 'ComplexRelationLookup' then FT.DisplayDescription else null end as 'Type.ComputedRelationshipLookup.Description.Display',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.HideHeader else null end as 'Type.ComputedRelationshipLookup.HideHeader',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.HideFooter else null end as 'Type.ComputedRelationshipLookup.HideFooter',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.HideFilter else null end as 'Type.ComputedRelationshipLookup.HideFilter',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.LookupType else null end as 'Type.ComputedRelationshipLookup.LookupType',

		        JSON_QUERY(case when FT.Type = 'ComplexRelationLookup' then (
		        select	IST.Uid as IntersectTypeUid,
				        AST.Uid as AssetTypeUid,
				        DR.RelationType,
				        DR.Direction
		        from	OPENJSON(FTL.Definition) with (Relations nvarchar(max) as json) D
				        outer apply OPENJSON(D.Relations) with (IntersectTypeID int, Object varchar(50), ObjectID int, RelationType int, Direction int) DR
				        left join IntersectType IST on IST.ID = DR.IntersectTypeID
				        left join AssetType AST on AST.Object = DR.Object and AST.ObjectID = DR.ObjectID
		        for json path
		        ) else null end) as 'Type.ComputedRelationshipLookup.Definition.Relations',
		        JSON_QUERY(case when FT.Type = 'ComplexRelationLookup' then (
		        select	AST.Uid as AssetTypeUid,
				        coalesce(AFT.Name, DF.FieldTypeName) as FieldTypeName,
				        DF.[Filter],
				        DF.OverrideDisplayName,
				        DF.DisplayOrder,
				        DF.SortOrder,
				        DF.Show,
				        DF.Width
		        from	OPENJSON(FTL.Definition) with (Fields nvarchar(max) as json) D
				        outer apply OPENJSON(D.Fields) with (Object varchar(50), ObjectID int, FieldTypeID int, FieldTypeName nvarchar(250), [Filter] nvarchar(500), OverrideDisplayName nvarchar(250), DisplayOrder int, SortOrder int, Show bit, Width int) DF
				        left join AssetType AST on AST.Object = DF.Object and AST.ObjectID = DF.ObjectID
				        left join FieldType AFT on AFT.ID = DF.FieldTypeID
		        order by DF.DisplayOrder
		        for json path
		        ) else null end) as 'Type.ComputedRelationshipLookup.Definition.Fields',
		        case when FT.Type = 'ComplexRelationLookup' then FT.IsDisplayable else null end as 'Type.ComputedRelationshipLookup.IsDisplayable',
		        case when FT.Type = 'ComplexRelationLookup' then FT.ShowIfEmpty else null end as 'Type.ComputedRelationshipLookup.ShowIfEmpty',

		        case when FT.Type = 'RefListRelationship' then FT.ColumnOrder else null end as 'Type.ComputedRelationshipReferenceList.ColumnOrder',
		        case when FT.Type = 'RefListRelationship' then FT.DisplayDescription else null end as 'Type.ComputedRelationshipReferenceList.Description.Display',
		        case when FT.Type = 'RefListRelationship' then IT.Uid else null end as 'Type.ComputedRelationshipReferenceList.IntersectTypeUid',
		        case when FT.Type = 'RefListRelationship' then FT.IsDisplayable else null end as 'Type.ComputedRelationshipReferenceList.IsDisplayable',
		        case when FT.Type = 'RefListRelationship' then FT.ShowIfEmpty else null end as 'Type.ComputedRelationshipReferenceList.ShowIfEmpty',

		        case when FT.Type = 'Date' then FT.ColumnOrder else null end as 'Type.Date.ColumnOrder',
		        case when FT.Type = 'Date' then FT.ColumnWidth else null end as 'Type.Date.ColumnWidth',
		        case when FT.Type = 'Date' then FT.SortOrder else null end as 'Type.Date.SortOrder',
		        case when FT.Type = 'Date' then TRY_CAST(FT.DefaultValue as date) else null end as 'Type.Date.DefaultValue',
		        case when FT.Type = 'Date' then FT.DisplayDescription else null end as 'Type.Date.Description.Display',
		        case when FT.Type = 'Date' then FT.FormDescription else null end as 'Type.Date.Description.Form',
		        case when FT.Type = 'Date' then FT.IsRequired else null end as 'Type.Date.Validation.IsRequired',
		        case when FT.Type = 'Date' then FT.IsDisplayable else null end as 'Type.Date.IsDisplayable',
		        case when FT.Type = 'Date' then FT.IsEditable else null end as 'Type.Date.IsEditable',
		        case when FT.Type = 'Date' then FT.IsListable else null end as 'Type.Date.IsListable',
		        case when FT.Type = 'Date' then FT.IsPartOfKey else null end as 'Type.Date.IsPartOfKey',
		        case when FT.Type = 'Date' then FT.IsPrimaryFilter else null end as 'Type.Date.IsPrimaryFilter',
		        case when FT.Type = 'Date' then FT.ShowIfEmpty else null end as 'Type.Date.ShowIfEmpty',

		        case when FT.Type = 'DateTime' then FT.ColumnOrder else null end as 'Type.DateTime.ColumnOrder',
		        case when FT.Type = 'DateTime' then FT.ColumnWidth else null end as 'Type.DateTime.ColumnWidth',
		        case when FT.Type = 'DateTime' then FT.SortOrder else null end as 'Type.DateTime.SortOrder',
		        case when FT.Type = 'DateTime' then TRY_CAST(FT.DefaultValue as datetime) else null end as 'Type.DateTime.DefaultValue',
		        case when FT.Type = 'DateTime' then FT.DisplayDescription else null end as 'Type.DateTime.Description.Display',
		        case when FT.Type = 'DateTime' then FT.FormDescription else null end as 'Type.DateTime.Description.Form',
		        case when FT.Type = 'DateTime' then FT.IsRequired else null end as 'Type.DateTime.Validation.IsRequired',
		        case when FT.Type = 'DateTime' then FT.IsDisplayable else null end as 'Type.DateTime.IsDisplayable',
		        case when FT.Type = 'DateTime' then FT.IsEditable else null end as 'Type.DateTime.IsEditable',
		        case when FT.Type = 'DateTime' then FT.IsListable else null end as 'Type.DateTime.IsListable',
		        case when FT.Type = 'DateTime' then FT.IsPartOfKey else null end as 'Type.DateTime.IsPartOfKey',
		        case when FT.Type = 'DateTime' then FT.IsPrimaryFilter else null end as 'Type.DateTime.IsPrimaryFilter',
		        case when FT.Type = 'DateTime' then FT.ShowIfEmpty else null end as 'Type.DateTime.ShowIfEmpty',

		        case when FT.Type = 'Decimal' then FT.ColumnOrder else null end as 'Type.Decimal.ColumnOrder',
		        case when FT.Type = 'Decimal' then FT.ColumnWidth else null end as 'Type.Decimal.ColumnWidth',
		        case when FT.Type = 'Decimal' then FT.SortOrder else null end as 'Type.Decimal.SortOrder',
		        case when FT.Type = 'Decimal' then TRY_CAST(FT.DefaultValue as decimal) else null end as 'Type.Decimal.DefaultValue',
		        case when FT.Type = 'Decimal' then FT.DisplayDescription else null end as 'Type.Decimal.Description.Display',
		        case when FT.Type = 'Decimal' then FT.FormDescription else null end as 'Type.Decimal.Description.Form',
		        case when FT.Type = 'Decimal' then FT.Increment else null end as 'Type.Decimal.Increment',
		        case when FT.Type = 'Decimal' then FT.MinimumLength else null end as 'Type.Decimal.Validation.MinimumLength',
		        case when FT.Type = 'Decimal' then FT.MaximumLength else null end as 'Type.Decimal.Validation.MaximumLength',
		        case when FT.Type = 'Decimal' then FT.[Precision] else null end as 'Type.Decimal.Validation.Precision',
		        case when FT.Type = 'Decimal' then FT.IsRequired else null end as 'Type.Decimal.Validation.IsRequired',
		        case when FT.Type = 'Decimal' then FT.IsDisplayable else null end as 'Type.Decimal.IsDisplayable',
		        case when FT.Type = 'Decimal' then FT.IsEditable else null end as 'Type.Decimal.IsEditable',
		        case when FT.Type = 'Decimal' then FT.IsListable else null end as 'Type.Decimal.IsListable',
		        case when FT.Type = 'Decimal' then FT.IsPartOfKey else null end as 'Type.Decimal.IsPartOfKey',
		        case when FT.Type = 'Decimal' then FT.ShowIfEmpty else null end as 'Type.Decimal.ShowIfEmpty',

		        case when FT.Type = 'Html' then FT.ColumnOrder else null end as 'Type.Html.ColumnOrder',
		        case when FT.Type = 'Html' then FT.ColumnWidth else null end as 'Type.Html.ColumnWidth',
		        case when FT.Type = 'Html' then FT.SortOrder else null end as 'Type.Html.SortOrder',
		        case when FT.Type = 'Html' then FT.DefaultValue else null end as 'Type.Html.DefaultValue',
		        case when FT.Type = 'Html' then FT.DisplayDescription else null end as 'Type.Html.Description.Display',
		        case when FT.Type = 'Html' then FT.FormDescription else null end as 'Type.Html.Description.Form',
		        case when FT.Type = 'Html' then FT.MinimumLength else null end as 'Type.Html.Validation.MinimumLength',
		        case when FT.Type = 'Html' then FT.MaximumLength else null end as 'Type.Html.Validation.MaximumLength',
		        case when FT.Type = 'Html' then FT.IsRequired else null end as 'Type.Html.Validation.IsRequired',
		        case when FT.Type = 'Html' then FT.IsDisplayable else null end as 'Type.Html.IsDisplayable',
		        case when FT.Type = 'Html' then FT.IsEditable else null end as 'Type.Html.IsEditable',
		        case when FT.Type = 'Html' then FT.IsListable else null end as 'Type.Html.IsListable',
		        case when FT.Type = 'Html' then FT.IsPartOfKey else null end as 'Type.Html.IsPartOfKey',
		        case when FT.Type = 'Html' then FT.IsPrimaryFilter else null end as 'Type.Html.IsPrimaryFilter',
		        case when FT.Type = 'Html' then FT.ShowIfEmpty else null end as 'Type.Html.ShowIfEmpty',

		        case when FT.Type = 'Json' then FT.ColumnOrder else null end as 'Type.Json.ColumnOrder',
		        case when FT.Type = 'Json' then FT.DisplayDescription else null end as 'Type.Json.Description.Display',
		        case when FT.Type = 'Json' then FT.IsDisplayable else null end as 'Type.Json.IsDisplayable',
		        case when FT.Type = 'Json' then FT.IsEditable else null end as 'Type.Json.IsEditable',
		        case when FT.Type = 'Json' then FT.ShowIfEmpty else null end as 'Type.Json.ShowIfEmpty',

		        case when FT.Type = 'Link' then FT.ColumnOrder else null end as 'Type.Link.ColumnOrder',
		        case when FT.Type = 'Link' then FT.ColumnWidth else null end as 'Type.Link.ColumnWidth',
		        case when FT.Type = 'Link' then FT.SortOrder else null end as 'Type.Link.SortOrder',
		        case when FT.Type = 'Link' then case when CHARINDEX('|', FT.DefaultValue, 1) > 1 then SUBSTRING(FT.DefaultValue, 1, CHARINDEX('|', FT.DefaultValue, 1)-1) else FT.DEfaultValue end else null end as 'Type.Link.DefaultValue.Text',
		        case when FT.Type = 'Link' then case when CHARINDEX('|', FT.DefaultValue, 1) > 1 then SUBSTRING(FT.DefaultValue, CHARINDEX('|', FT.DefaultValue, 1)+1, LEN(FT.DefaultValue)-CHARINDEX('|', FT.DefaultValue, 1)) else FT.DEfaultValue end else null end as 'Type.Link.DefaultValue.Url',
		        case when FT.Type = 'Link' then FT.DisplayDescription else null end as 'Type.Link.Description.Display',
		        case when FT.Type = 'Link' then FT.FormDescription else null end as 'Type.Link.Description.Form',
		        case when FT.Type = 'Link' then FT.IsRequired else null end as 'Type.Link.Validation.IsRequired',
		        case when FT.Type = 'Link' then FT.IsDisplayable else null end as 'Type.Link.IsDisplayable',
		        case when FT.Type = 'Link' then FT.IsEditable else null end as 'Type.Link.IsEditable',
		        case when FT.Type = 'Link' then FT.IsListable else null end as 'Type.Link.IsListable',
		        case when FT.Type = 'Link' then FT.ShowIfEmpty else null end as 'Type.Link.ShowIfEmpty',

		        case when FT.Type = 'Lookup' then FT.ColumnOrder else null end as 'Type.Lookup.ColumnOrder',
		        case when FT.Type = 'Lookup' then FT.ColumnWidth else null end as 'Type.Lookup.ColumnWidth',
		        case when FT.Type = 'Lookup' then FT.SortOrder else null end as 'Type.Lookup.SortOrder',
		        case when FT.Type = 'Lookup' then DFA.[Uid] else null end as 'Type.Lookup.DefaultValue',
		        case when FT.Type = 'Lookup' then FT.DisplayDescription else null end as 'Type.Lookup.Description.Display',
		        case when FT.Type = 'Lookup' then FT.FormDescription else null end as 'Type.Lookup.Description.Form',
                case when FT.Type = 'Lookup' then FT.IsRequired else null end as 'Type.Lookup.Validation.IsRequired',
		        case when FT.Type = 'Lookup' then coalesce(try_cast(FT.AllowAllValue as bit), cast(0 as bit)) else null end as 'Type.Lookup.AllowAllValue',
		        case when FT.Type = 'Lookup' then case when try_cast(FT.AllowAllValue as bit) = 1 then FT.AllowAllLabel else null end else null end as 'Type.Lookup.AllowAllLabel',
		        case when FT.Type = 'Lookup' then FilterFT.[Name] else null end as 'Type.Lookup.Filter.FieldTypeName',
		        case when FT.Type = 'Lookup' then FilterPT.[Uid] else null end as 'Type.Lookup.Filter.PredicateUid',
		        case when FT.Type = 'Lookup' then FT.FilterPredicateDirection else null end as 'Type.Lookup.Filter.UseDirection',
		        case when FT.Type = 'Lookup' then FT.LookupDisplayFormat else null end as 'Type.Lookup.Format.Display',
		        case when FT.Type = 'Lookup' then FT.LookupEditFormat else null end as 'Type.Lookup.Format.Edit',
		        case when FT.Type = 'Lookup' and FT.LookupObjectType not in ('ReferenceItemType', 'TaxonomyType') then LookupOT.Uid else null end as 'Type.Lookup.List.Uid',
				case 
					when FT.Type = 'Lookup' and LookupOT.Uid is not null and FT.LookupObjectType not in ('ReferenceItemType', 'TaxonomyType') then LookupOT.Class 
					when FT.Type = 'Lookup' and FT.LookupObjectType = 'TaxonomyType' and FT.LookupObjectID = 0 then 2
					when FT.Type = 'Lookup' and FT.LookupObjectType = 'ReferenceItemType' and FT.LookupObjectID = 0 then 9
					when FT.Type = 'Lookup' and LookupOT.Uid is null and FT.LookupObjectID <> 0 then 0 
                    else null 
				end as 'Type.Lookup.List.Class',
		        case when FT.Type = 'Lookup' then FT.AllowMultipleValues else null end as 'Type.Lookup.List.AllowMultipleValues',
		        case when FT.Type = 'Lookup' then FT.IsDisplayable else null end as 'Type.Lookup.IsDisplayable',
		        case when FT.Type = 'Lookup' then FT.IsEditable else null end as 'Type.Lookup.IsEditable',
		        case when FT.Type = 'Lookup' then FT.IsListable else null end as 'Type.Lookup.IsListable',
		        case when FT.Type = 'Lookup' then FT.IsPartOfKey else null end as 'Type.Lookup.IsPartOfKey',
		        case when FT.Type = 'Lookup' then FT.IsPrimaryFilter else null end as 'Type.Lookup.IsPrimaryFilter',
		        case when FT.Type = 'Lookup' then FT.ShowIfEmpty else null end as 'Type.Lookup.ShowIfEmpty',

		        case when FT.Type = 'Number' then FT.ColumnOrder else null end as 'Type.Number.ColumnOrder',
		        case when FT.Type = 'Number' then FT.ColumnWidth else null end as 'Type.Number.ColumnWidth',
		        case when FT.Type = 'Number' then FT.SortOrder else null end as 'Type.Number.SortOrder',
		        case when FT.Type = 'Number' then TRY_CAST(FT.DefaultValue as int) else null end as 'Type.Number.DefaultValue',
		        case when FT.Type = 'Number' then FT.DisplayDescription else null end as 'Type.Number.Description.Display',
		        case when FT.Type = 'Number' then FT.FormDescription else null end as 'Type.Number.Description.Form',
		        case when FT.Type = 'Number' then FT.Increment else null end as 'Type.Number.Increment',
		        case when FT.Type = 'Number' then FT.MinimumLength else null end as 'Type.Number.Validation.MinimumLength',
		        case when FT.Type = 'Number' then FT.MaximumLength else null end as 'Type.Number.Validation.MaximumLength',
		        case when FT.Type = 'Number' then FT.IsRequired else null end as 'Type.Number.Validation.IsRequired',
		        case when FT.Type = 'Number' then FT.IsDisplayable else null end as 'Type.Number.IsDisplayable',
		        case when FT.Type = 'Number' then FT.IsEditable else null end as 'Type.Number.IsEditable',
		        case when FT.Type = 'Number' then FT.IsListable else null end as 'Type.Number.IsListable',
		        case when FT.Type = 'Number' then FT.IsPartOfKey else null end as 'Type.Number.IsPartOfKey',
		        case when FT.Type = 'Number' then FT.IsPrimaryFilter else null end as 'Type.Number.IsPrimaryFilter',
		        case when FT.Type = 'Number' then FT.ShowIfEmpty else null end as 'Type.Number.ShowIfEmpty',

		        case when FT.Type = 'Relationship' then FT.ColumnOrder else null end as 'Type.Relationship.ColumnOrder',
		        case when FT.Type = 'Relationship' then FT.ColumnWidth else null end as 'Type.Relationship.ColumnWidth',
		        case when FT.Type = 'Relationship' then FT.SortOrder else null end as 'Type.Relationship.SortOrder',
		        case when FT.Type = 'Relationship' then FT.DisplayDescription else null end as 'Type.Relationship.Description.Display',
		        case when FT.Type = 'Relationship' then FT.FormDescription else null end as 'Type.Relationship.Description.Form',
		        case when FT.Type = 'Relationship' then IT.Uid else null end as 'Type.Relationship.IntersectTypeUid',
		        case when FT.Type = 'Relationship' then FT.IsRequired else null end as 'Type.Relationship.Validation.IsRequired',
		        case when FT.Type = 'Relationship' then FT.IsDisplayable else null end as 'Type.Relationship.IsDisplayable',
		        case when FT.Type = 'Relationship' then FT.IsEditable else null end as 'Type.Relationship.IsEditable',
		        case when FT.Type = 'Relationship' then FT.IsListable else null end as 'Type.Relationship.IsListable',
		        case when FT.Type = 'Relationship' then FT.IsPrimaryFilter else null end as 'Type.Relationship.IsPrimaryFilter',
		        case when FT.Type = 'Relationship' then FT.ShowIfEmpty else null end as 'Type.Relationship.ShowIfEmpty',

		        case when FT.Type = 'Text' then FT.ColumnOrder else null end as 'Type.Text.ColumnOrder',
		        case when FT.Type = 'Text' then FT.ColumnWidth else null end as 'Type.Text.ColumnWidth',
		        case when FT.Type = 'Text' then FT.SortOrder else null end as 'Type.Text.SortOrder',
		        case when FT.Type = 'Text' then FT.DefaultValue else null end as 'Type.Text.DefaultValue',
		        case when FT.Type = 'Text' then FT.DisplayDescription else null end as 'Type.Text.Description.Display',
		        case when FT.Type = 'Text' then FT.FormDescription else null end as 'Type.Text.Description.Form',
		        case when FT.Type = 'Text' then FT.MinimumLength else null end as 'Type.Text.Validation.MinimumLength',
		        case when FT.Type = 'Text' then FT.MaximumLength else null end as 'Type.Text.Validation.MaximumLength',
		        case when FT.Type = 'Text' then FT.Pattern else null end as 'Type.Text.Validation.Pattern',
		        case when FT.Type = 'Text' then FT.IsRequired else null end as 'Type.Text.Validation.IsRequired',
		        case when FT.Type = 'Text' then FT.ValidationDescription else null end as 'Type.Text.Validation.Message',
		        case when FT.Type = 'Text' then FT.IsDisplayable else null end as 'Type.Text.IsDisplayable',
		        case when FT.Type = 'Text' then FT.IsEditable else null end as 'Type.Text.IsEditable',
		        case when FT.Type = 'Text' then FT.IsListable else null end as 'Type.Text.IsListable',
		        case when FT.Type = 'Text' then FT.IsPartOfKey else null end as 'Type.Text.IsPartOfKey',
		        case when FT.Type = 'Text' then FT.IsPrimaryFilter else null end as 'Type.Text.IsPrimaryFilter',
		        case when FT.Type = 'Text' then FT.ShowIfEmpty else null end as 'Type.Text.ShowIfEmpty'
        from	FieldType FT
				left join AssetType O_A on O_A.ID = FT.AssetTypeID 
				left join IssueType O_I on FT.Object = 'IssueType' and O_I.ID = FT.ObjectID 
				left join IntersectType O_R on FT.Object = 'IntersectType' and O_R.ID = FT.ObjectID 
                
                left join FieldTypeLookup FTL on FTL.FieldTypeID = FT.ID
		        left join IntersectType IT on (FT.[Type] = 'FieldFromRelationship' or FT.[Type] = 'RefListRelationship' or FT.[Type] = 'Relationship') and FT.LookupObjectType = 'IntersectType' and IT.ID = FT.LookupObjectID
		        left join FieldType LFT on FT.[Type] = 'FieldFromRelationship' and LFT.ID = FT.LookupObjectFieldTypeID

		        left join FieldType FilterFT on FT.[Type] = 'Lookup' and FilterFT.ID = FT.FilterFieldTypeID
		        left join [Predicate] FilterPT on FT.[Type] = 'Lookup' and FilterPT.ID = FT.FilterPredicateID
		        left join [AssetType] LookupOT on FT.[Type] = 'Lookup' 
													and LookupOT.[Object] = case FT.LookupObjectType 
																				when 'ReferenceItemType' then FT.LookupObjectType 
																				else FT.LookupObjectType+'Type' 
																			end 
													and LookupOT.ObjectID = FT.LookupObjectID 
				outer apply (
							select	top 1 
									Uid 
							from	Asset 
							where	FT.[Type] = 'Lookup' 
									and Object = FT.LookupObjectType 
									and ObjectID = try_cast(FT.DefaultValue as int)
							) DFA 
        {whereClause}
        order by FT.Object, FT.ObjectID, FT.Name
        offset ((@pageNum-1) * @pageSize) rows fetch next @pageSize rows only
        for json path
        ) as 'items'
for json path, WITHOUT_ARRAY_WRAPPER";

            var model = await Company.GetDatabaseJsonAsObjectAsync<FieldTypesApiViewModel>(sql, dbArgs);

            return model;
        }

        /// <summary>
        /// Retrieves field types contained within your environment.
        /// </summary>
        /// <remarks>
        /// If using Uid parameters, you may only provide one of the following: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.
        /// </remarks>
        /// <param name="AssetTypeUid">The asset type Uid to retrieve field types for.</param>
        /// <param name="RelationshipTypeUid">The relationship type Uid to retrieve field types for.</param>
        /// /// <param name="ActionTypeUid">The action type Uid to retrieve field types for.</param>
        /// <param name="Name">The API Name to search for.</param>
        /// <param name="FriendlyName">The Friendly Name to search for.</param>
        /// <param name="Type">The data type to search for.</param>
        /// <param name="_pageSize">The number of results to return per page. The default value is 200.</param>
        /// <param name="_pageNum">The page number to return results for.</param>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpGet,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(FieldTypesApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetFieldTypesAsync(Guid? AssetTypeUid = null, Guid? RelationshipTypeUid = null, Guid? ActionTypeUid = null, 
            string Name = "", string FriendlyName = "", DataType? Type = null, int? _pageSize = null, int? _pageNum = null)
        {
            var prefix = "Fields.GetFieldTypesAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var results = await GetFieldTypes(queryParams);
                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (RestApiException ex)
            {
                errorMessage = ex.GetFullExceptionData(false);
                return ReturnApiError(ex.Status, errorMessage);
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

        /// <summary>
        /// Adds or updates field types contained within your environment based on a specified ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.
        /// </summary>
        /// <remarks>
        /// You may only provide one of the following: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.
        /// </remarks>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpPut,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutFieldTypesAsync(FieldTypesApiEditModel model)
        {
            var prefix = "Fields.PutFieldTypesAsync => ";
            var errorMessage = "";

            try
            {
                IEnumerable<TypeIdentifierInfoModel> typeIdentifierInfoModels = null;
                TypeIdentifierInfoModel typeIdentifierInfoModel = null;

                #region Validation

                if (model == null)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "No model found", "You did not provide a valid model. Please check your request and try again.");
                }

                if (!model.ActionTypeUid.HasValue && !model.AssetTypeUid.HasValue && !model.RelationshipTypeUid.HasValue)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "No Uid found", "You must provide only one of the three Uid properties: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.");
                }

                if (model.ActionTypeUid.HasValue)
                {
                    typeIdentifierInfoModels = await Company.QueryAsync<TypeIdentifierInfoModel>("select ID, Uid, 'IssueType' as Object, ID as ObjectID from IssueType where Uid = @uid", new { uid = model.ActionTypeUid.Value });
                    typeIdentifierInfoModel = typeIdentifierInfoModels.SingleOrDefault();
                    if (typeIdentifierInfoModel == null)
                    {
                        throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Action Type not found based on Uid provided [{model.ActionTypeUid}].");
                    }
                }

                if (model.AssetTypeUid.HasValue)
                {
                    if (model.ActionTypeUid.HasValue)
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an AssetTypeUid since you have already provided an ActionTypeUid.");
                    }
                    else
                    {
                        typeIdentifierInfoModels = await Company.QueryAsync<TypeIdentifierInfoModel>("select ID, Uid, Object, ObjectID from AssetType where Uid = @uid", new { uid = model.AssetTypeUid.Value });
                        typeIdentifierInfoModel = typeIdentifierInfoModels.SingleOrDefault();
                        if (typeIdentifierInfoModel == null)
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Asset Type not found based on Uid provided [{model.ActionTypeUid}].");
                        }
                    }
                }

                if (model.RelationshipTypeUid.HasValue)
                {
                    if (model.ActionTypeUid.HasValue)
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an ActionTypeUid.");
                    }
                    else if (model.AssetTypeUid.HasValue)
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an AssetTypeUid.");
                    }
                    else
                    {
                        typeIdentifierInfoModels = await Company.QueryAsync<TypeIdentifierInfoModel>("select ID, Uid, 'IntersectType' as Object, ID as ObjectID from IntersectType where Uid = @uid", new { uid = model.RelationshipTypeUid.Value });
                        typeIdentifierInfoModel = typeIdentifierInfoModels.SingleOrDefault();
                        if (typeIdentifierInfoModel == null)
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Relationship Type not found based on Uid provided [{model.RelationshipTypeUid}].");
                        }
                    }
                }

                #region Security check

                bool hasPermissions = false;

                if (Company.CurrentResourceIsAdmin)
                {
                    hasPermissions = true;
                }
                else
                {
                    var typePermissions = Company.GetTypePermissions(typeIdentifierInfoModel.Object, typeIdentifierInfoModel.ObjectID);
                    if (typePermissions != null)
                    {
                        hasPermissions = typePermissions.Any(i => i.ID == Permission.ModifyAsset);
                    }
                }

                if (!hasPermissions)
                {
                    throw new RestApiException(HttpStatusCode.Unauthorized, "Not authorized", "You do not have permissions to remove fields on this type.");
                }

                #endregion

                bool actionIsReplaceAndKeySelected = (model.Action == FieldTypesApiEditAction.Merge); //If set to merge we can set to true and skip this step.
                bool fieldsHaveErrors = false;
                var fieldsHaveErrorsList = new List<string>();
                foreach (var field in model.Fields)
                {
                    if (!field.Type.IsOnlyOneTypeModelDefined())
                    {
                        fieldsHaveErrors = true;
                        fieldsHaveErrorsList.Add(field.Name);
                    }
                    if (model.Action == FieldTypesApiEditAction.Replace)
                    {
                        if (field.Type.IsPartyOfKey())
                        {
                            actionIsReplaceAndKeySelected = true;
                        }
                    }
                }
                if (fieldsHaveErrors)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Fields contain errors", $"The following fields have more than one type defined: {string.Join(", ", fieldsHaveErrorsList)}.");
                }
                if (!actionIsReplaceAndKeySelected)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "No primary key defined", $"You have elected to replace all current fields, yet you have not defined a key. You must define at least one field as a key, or choose Merge as an Action.");
                }
                var duplicateFieldNames = model.Fields.Select(f => f.Name).GroupBy(f => f).Where(f => f.Count() > 1).Select(f => f.Key).ToList();
                if (duplicateFieldNames.Count > 0)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Duplicate field names", $"The following field names are used more than once: {string.Join(", ", fieldsHaveErrorsList)}. Field names must be unique.");
                }

                if (model.Action == FieldTypesApiEditAction.Replace)
                {
                    // This is a full replace, so we need to validate that there are no current assets before we allow this.
                    var anyExistingItems = false;
                    switch (typeIdentifierInfoModel.Object)
                    {
                        case "IntersectType":
                            anyExistingItems = Company.Any<Intersect>(i => i.IntersectTypeID == typeIdentifierInfoModel.ID);
                            break;
                        case "IssueType":
                            anyExistingItems = Company.Any<Issue>(i => i.IssueTypeID == typeIdentifierInfoModel.ID);
                            break;
                        default:
                            anyExistingItems = Company.Any<Asset>(i => i.AssetTypeID == typeIdentifierInfoModel.ID);
                            break;
                    }
                    if (anyExistingItems)
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, "Existing items in system", $"There are existing items in your environment. You may not perform a Replace action until those items are removed.");
                    }
                }

                #endregion

                #region Validation done, time to do some work

                var currentFieldTypes = Company.Filter<FieldType>(f => f.Object == typeIdentifierInfoModel.Object && f.ObjectID == typeIdentifierInfoModel.ObjectID, i => i.FieldTypeLookup).ToList();

                var newFieldTypes = new List<FieldType>();

                var fieldTypeNamesToDelete = new List<string>();
                var allowedConversions = DataType.Boolean.GetAllowedConversionOptions();
                model.Fields.ForEach(f =>
                {
                    var newFieldType = new FieldType
                    {
                        AssetTypeID = typeIdentifierInfoModel.ID,
                        Object = typeIdentifierInfoModel.Object,
                        ObjectID = typeIdentifierInfoModel.ObjectID,
                        Category = f.Category,
                        Name = f.Name,
                        FriendlyName = f.FriendlyName
                    };

                    if (f.Type.Boolean != null)
                    {
                        newFieldType.Type = DataType.Boolean.ToString();
                        newFieldType.ColumnOrder = f.Type.Boolean.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.Boolean.ColumnWidth;
                        if (f.Type.Boolean.DefaultValue.HasValue) newFieldType.DefaultValue = f.Type.Boolean.DefaultValue.Value.ToString().ToLower();
                        if (f.Type.Boolean.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.Boolean.Description.Display;
                            newFieldType.FormDescription = f.Type.Boolean.Description.Form;
                        }
                        newFieldType.IsDisplayable = f.Type.Boolean.IsDisplayable;
                        newFieldType.IsEditable = f.Type.Boolean.IsEditable;
                        newFieldType.IsListable = f.Type.Boolean.IsListable;
                        newFieldType.IsPartOfKey = f.Type.Boolean.IsPartOfKey;
                        newFieldType.IsPrimaryFilter = f.Type.Boolean.IsPrimaryFilter;
                        newFieldType.ShowIfEmpty = f.Type.Boolean.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.Boolean.SortOrder;
                    }
                    else if (f.Type.ComputedFusionLookup != null)
                    {
                        if (model.ActionTypeUid.HasValue || model.RelationshipTypeUid.HasValue)
                        {
                            throw new RestApiException(HttpStatusCode.BadRequest, "Field type error", $"You may not use a Fusion Lookup type on an action type or relationship type for field {f.Name}.");
                        }

                        newFieldType.Type = DataType.FusionLookup.ToString();
                        newFieldType.ColumnOrder = f.Type.ComputedFusionLookup.ColumnOrder;
                        if (f.Type.ComputedFusionLookup.Description != null) newFieldType.DisplayDescription = f.Type.ComputedFusionLookup.Description.Display;
                        newFieldType.IsDisplayable = f.Type.ComputedFusionLookup.IsDisplayable;
                        newFieldType.IsEditable = false;
                        newFieldType.IsListable = false;
                        newFieldType.IsPartOfKey = false;
                        newFieldType.IsPrimaryFilter = false;
                        newFieldType.ShowIfEmpty = false;
                        newFieldType.SortOrder = 99;
                    }
                    else if (f.Type.ComputedOwnershipLookup != null)
                    {
                        if (model.ActionTypeUid.HasValue || model.RelationshipTypeUid.HasValue)
                        {
                            throw new RestApiException(HttpStatusCode.BadRequest, "Field type error", $"You may not use a Ownership Lookup type on an action type or relationship type for field {f.Name}.");
                        }

                        newFieldType.Type = DataType.OwnershipLookup.ToString();
                        newFieldType.ColumnOrder = f.Type.ComputedOwnershipLookup.ColumnOrder;
                        if (f.Type.ComputedOwnershipLookup.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.ComputedOwnershipLookup.Description.Display;
                        }
                        newFieldType.IsDisplayable = f.Type.ComputedOwnershipLookup.IsDisplayable;
                        newFieldType.IsEditable = false;
                        newFieldType.IsListable = false;
                        newFieldType.IsPartOfKey = false;
                        newFieldType.IsPrimaryFilter = false;
                        newFieldType.ShowIfEmpty = f.Type.ComputedOwnershipLookup.ShowIfEmpty;
                        newFieldType.SortOrder = 99;

                        newFieldType.FieldTypeLookup = new FieldTypeLookup
                        {
                            HideFilter = f.Type.ComputedOwnershipLookup.HideFilter,
                            HideFooter = f.Type.ComputedOwnershipLookup.HideFooter,
                            HideHeader = f.Type.ComputedOwnershipLookup.HideHeader,
                            LookupType = 0,
                            Definition = JsonConvert.SerializeObject(f.Type.ComputedOwnershipLookup.Definition)
                        };
                    }
                    else if (f.Type.ComputedRelationshipField != null)
                    {
                        newFieldType.Type = DataType.FieldFromRelationship.ToString();
                        newFieldType.ColumnOrder = f.Type.ComputedRelationshipField.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.ComputedRelationshipField.ColumnWidth;
                        if (f.Type.ComputedRelationshipField.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.ComputedRelationshipField.Description.Display;
                        }
                        var relationshipsFieldType = Company.Query<dynamic>(@"
select	I.ID as IntersectTypeID,
		F.ID as FieldTypeID
from	IntersectType I
		inner join FieldType F on 
				F.Object = case 
								when (@t = I.Subject and @tid = I.SubjectID) then I.Object 
								else I.Subject end 
				and F.ObjectID = case 
								when (@t = I.Subject and @tid = I.SubjectID) then I.ObjectID 
								else I.SubjectID 
							end 
				and I.Uid = @uid 
				and F.Name = @n", new
                        {
                            uid = f.Type.ComputedRelationshipField.IntersectTypeUid,
                            n = f.Type.ComputedRelationshipField.FieldTypeName,
                            t = typeIdentifierInfoModel.Object,
                            tid = typeIdentifierInfoModel.ObjectID
                        }).FirstOrDefault();
                        if (relationshipsFieldType == null)
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Relationship Type/Field not found", $"Relationship Type or Field Type not found based on Uid provided [{f.Type.ComputedRelationshipField.IntersectTypeUid}].");
                        }
                        newFieldType.LookupObjectType = "IntersectType";
                        newFieldType.LookupObjectID = relationshipsFieldType.IntersectTypeID;
                        newFieldType.LookupObjectFieldTypeID = relationshipsFieldType.FieldTypeID;
                        newFieldType.IsDisplayable = f.Type.ComputedRelationshipField.IsDisplayable;
                        newFieldType.IsEditable = false;
                        newFieldType.IsListable = f.Type.ComputedRelationshipField.IsListable;
                        newFieldType.IsPartOfKey = false;
                        newFieldType.IsPrimaryFilter = false;
                        newFieldType.ShowIfEmpty = f.Type.ComputedRelationshipField.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.ComputedRelationshipField.SortOrder;
                    }
                    else if (f.Type.ComputedRelationshipLookup != null)
                    {
                        if (model.ActionTypeUid.HasValue || model.RelationshipTypeUid.HasValue)
                        {
                            throw new RestApiException(HttpStatusCode.BadRequest, "Field type error", $"You may not use a Relationship Lookup type on an action type or relationship type for field {f.Name}.");
                        }

                        newFieldType.Type = DataType.ComplexRelationLookup.ToString();
                        newFieldType.ColumnOrder = f.Type.ComputedRelationshipLookup.ColumnOrder;
                        if (f.Type.ComputedRelationshipLookup.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.ComputedRelationshipLookup.Description.Display;
                        }
                        newFieldType.IsDisplayable = f.Type.ComputedRelationshipLookup.IsDisplayable;
                        newFieldType.ShowIfEmpty = f.Type.ComputedRelationshipLookup.ShowIfEmpty;
                        newFieldType.FieldTypeLookup = new FieldTypeLookup
                        {
                            HideFilter = f.Type.ComputedRelationshipLookup.HideFilter,
                            HideFooter = f.Type.ComputedRelationshipLookup.HideFooter,
                            HideHeader = f.Type.ComputedRelationshipLookup.HideHeader,
                            LookupType = 0,
                            Definition = JsonConvert.SerializeObject(f.Type.ComputedRelationshipLookup.Definition)
                        };
                    }
                    else if (f.Type.ComputedRelationshipReferenceList != null)
                    {
                        if (model.ActionTypeUid.HasValue || model.RelationshipTypeUid.HasValue)
                        {
                            throw new RestApiException(HttpStatusCode.BadRequest, "Field type error", $"You may not use a Reference Item List from Relationship type on an action type or relationship type for field {f.Name}.");
                        }

                        newFieldType.Type = DataType.RefListRelationship.ToString();
                        newFieldType.ColumnOrder = f.Type.ComputedRelationshipReferenceList.ColumnOrder;
                        if (f.Type.ComputedRelationshipReferenceList.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.ComputedRelationshipReferenceList.Description.Display;
                        }
                        newFieldType.IsDisplayable = f.Type.ComputedRelationshipReferenceList.IsDisplayable;
                        newFieldType.ShowIfEmpty = f.Type.ComputedRelationshipReferenceList.ShowIfEmpty;
                        var relationshipsFieldType = Company.Query<int>(@"select ID from IntersectType where Uid = @uid", new { uid = f.Type.ComputedRelationshipReferenceList.IntersectTypeUid }).FirstOrDefault();
                        if (relationshipsFieldType <= 0)
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Relationship Type not found", $"Relationship Type or Field Type not found based on Uid provided [{f.Type.ComputedRelationshipReferenceList.IntersectTypeUid}].");
                        }
                        newFieldType.LookupObjectType = "IntersectType";
                        newFieldType.LookupObjectID = relationshipsFieldType;
                    }
                    else if (f.Type.Date != null)
                    {
                        newFieldType.Type = DataType.Date.ToString();
                        newFieldType.ColumnOrder = f.Type.Date.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.Date.ColumnWidth;
                        if (f.Type.Date.DefaultValue.HasValue) newFieldType.DefaultValue = f.Type.Date.DefaultValue.Value.ToString();
                        if (f.Type.Date.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.Date.Description.Display;
                            newFieldType.FormDescription = f.Type.Date.Description.Form;
                        }
                        newFieldType.IsDisplayable = f.Type.Date.IsDisplayable;
                        newFieldType.IsEditable = f.Type.Date.IsEditable;
                        newFieldType.IsListable = f.Type.Date.IsListable;
                        newFieldType.IsPartOfKey = f.Type.Date.IsPartOfKey;
                        newFieldType.IsPrimaryFilter = f.Type.Date.IsPrimaryFilter;
                        newFieldType.ShowIfEmpty = f.Type.Date.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.Date.SortOrder;
                        if (f.Type.Date.Validation != null)
                        {
                            newFieldType.IsRequired = f.Type.Date.Validation.IsRequired;
                        }
                    }
                    else if (f.Type.DateTime != null)
                    {
                        newFieldType.Type = DataType.DateTime.ToString();
                        newFieldType.ColumnOrder = f.Type.DateTime.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.DateTime.ColumnWidth;
                        if (f.Type.DateTime.DefaultValue.HasValue) newFieldType.DefaultValue = f.Type.DateTime.DefaultValue.Value.ToString();
                        if (f.Type.DateTime.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.DateTime.Description.Display;
                            newFieldType.FormDescription = f.Type.DateTime.Description.Form;
                        }
                        newFieldType.IsDisplayable = f.Type.DateTime.IsDisplayable;
                        newFieldType.IsEditable = f.Type.DateTime.IsEditable;
                        newFieldType.IsListable = f.Type.DateTime.IsListable;
                        newFieldType.IsPartOfKey = f.Type.DateTime.IsPartOfKey;
                        newFieldType.IsPrimaryFilter = f.Type.DateTime.IsPrimaryFilter;
                        newFieldType.ShowIfEmpty = f.Type.DateTime.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.DateTime.SortOrder;
                        if (f.Type.DateTime.Validation != null)
                        {
                            newFieldType.IsRequired = f.Type.DateTime.Validation.IsRequired;
                        }
                    }
                    else if (f.Type.Decimal != null)
                    {
                        newFieldType.Type = DataType.Decimal.ToString();
                        newFieldType.ColumnOrder = f.Type.Decimal.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.Decimal.ColumnWidth;
                        if (f.Type.Decimal.DefaultValue.HasValue) newFieldType.DefaultValue = f.Type.Decimal.DefaultValue.Value.ToString();
                        if (f.Type.Decimal.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.Decimal.Description.Display;
                            newFieldType.FormDescription = f.Type.Decimal.Description.Form;
                        }
                        newFieldType.IsDisplayable = f.Type.Decimal.IsDisplayable;
                        newFieldType.IsEditable = f.Type.Decimal.IsEditable;
                        newFieldType.IsListable = f.Type.Decimal.IsListable;
                        newFieldType.IsPartOfKey = f.Type.Decimal.IsPartOfKey;
                        newFieldType.IsPrimaryFilter = f.Type.Decimal.IsPrimaryFilter;
                        newFieldType.ShowIfEmpty = f.Type.Decimal.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.Decimal.SortOrder;
                        newFieldType.Increment = f.Type.Decimal.Increment;
                        if (f.Type.Decimal.Validation != null)
                        {
                            newFieldType.IsRequired = f.Type.Decimal.Validation.IsRequired;
                            newFieldType.MaximumLength = f.Type.Decimal.Validation.MaximumLength;
                            newFieldType.MinimumLength = f.Type.Decimal.Validation.MinimumLength;
                            newFieldType.Precision = f.Type.Decimal.Validation.Precision;
                        }
                    }
                    else if (f.Type.Html != null)
                    {
                        newFieldType.Type = DataType.Html.ToString();
                        newFieldType.ColumnOrder = f.Type.Html.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.Html.ColumnWidth;
                        newFieldType.DefaultValue = f.Type.Html.DefaultValue;
                        if (f.Type.Html.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.Html.Description.Display;
                            newFieldType.FormDescription = f.Type.Html.Description.Form;
                        }
                        newFieldType.IsDisplayable = f.Type.Html.IsDisplayable;
                        newFieldType.IsEditable = f.Type.Html.IsEditable;
                        newFieldType.IsListable = f.Type.Html.IsListable;
                        newFieldType.IsPartOfKey = f.Type.Html.IsPartOfKey;
                        newFieldType.IsPrimaryFilter = f.Type.Html.IsPrimaryFilter;
                        newFieldType.ShowIfEmpty = f.Type.Html.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.Html.SortOrder;
                        if (f.Type.Html.Validation != null)
                        {
                            newFieldType.IsRequired = f.Type.Html.Validation.IsRequired;
                            newFieldType.MaximumLength = f.Type.Html.Validation.MaximumLength;
                            newFieldType.MinimumLength = f.Type.Html.Validation.MinimumLength;
                        }
                    }
                    else if (f.Type.Json != null)
                    {
                        if (model.ActionTypeUid.HasValue || model.RelationshipTypeUid.HasValue)
                        {
                            throw new RestApiException(HttpStatusCode.BadRequest, "Field type error", $"You may not use a JSON type on an action type or relationship type for field {f.Name}.");
                        }
                        newFieldType.Type = DataType.JSON.ToString();
                        newFieldType.ColumnOrder = f.Type.Json.ColumnOrder;
                        if (f.Type.Json.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.Json.Description.Display;
                        }
                        newFieldType.IsDisplayable = f.Type.Json.IsDisplayable;
                        newFieldType.ShowIfEmpty = f.Type.Json.ShowIfEmpty;
                    }
                    else if (f.Type.Link != null)
                    {
                        newFieldType.Type = DataType.Link.ToString();
                        newFieldType.ColumnOrder = f.Type.Link.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.Link.ColumnWidth;
                        if (f.Type.Link.DefaultValue != null) newFieldType.DefaultValue = $"{f.Type.Link.DefaultValue.Text}|{f.Type.Link.DefaultValue.Url}";
                        if (f.Type.Link.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.Link.Description.Display;
                            newFieldType.FormDescription = f.Type.Link.Description.Form;
                        }
                        newFieldType.IsDisplayable = f.Type.Link.IsDisplayable;
                        newFieldType.IsEditable = f.Type.Link.IsEditable;
                        newFieldType.IsListable = f.Type.Link.IsListable;
                        newFieldType.IsPartOfKey = f.Type.Link.IsPartOfKey;
                        newFieldType.IsPrimaryFilter = f.Type.Link.IsPrimaryFilter;
                        newFieldType.ShowIfEmpty = f.Type.Link.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.Link.SortOrder;
                        if (f.Type.Link.Validation != null)
                        {
                            newFieldType.IsRequired = f.Type.Link.Validation.IsRequired;
                        }
                    }
                    else if (f.Type.Lookup != null)
                    {
                        newFieldType.Type = DataType.Lookup.ToString();
                        newFieldType.ColumnOrder = f.Type.Lookup.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.Lookup.ColumnWidth;
                        if (!string.IsNullOrEmpty(f.Type.Lookup.DefaultValue)) newFieldType.DefaultValue = f.Type.Lookup.DefaultValue.Trim();
                        if (f.Type.Lookup.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.Lookup.Description.Display;
                            newFieldType.FormDescription = f.Type.Lookup.Description.Form;
                        }
                        newFieldType.AllowAllLabel = f.Type.Lookup.AllowAllLabel;
                        newFieldType.AllowAllValue = f.Type.Lookup.AllowAllValue ?? false;
                        if (f.Type.Lookup.List != null)
                        {
                            newFieldType.AllowMultipleValues = f.Type.Lookup.List.AllowMultipleValues;
                            if (f.Type.Lookup.List.Class.HasValue && f.Type.Lookup.List.Uid.HasValue)
                            {
                                var listAssetType = Company.Filter<AssetType>(i => i.uid == f.Type.Lookup.List.Uid.Value).SingleOrDefault();
                                if (listAssetType != null)
                                {
                                    newFieldType.LookupObjectType = listAssetType.Object.Replace("Type", "");
                                    newFieldType.LookupObjectID = listAssetType.ObjectID;
                                }
                                else
                                {
                                    throw new RestApiException(HttpStatusCode.NotFound, "List Asset Type not found", $"Asset Type not found for field [{f.Name}].");
                                }
                            }
                            else if (f.Type.Lookup.List.Class.HasValue && !f.Type.Lookup.List.Uid.HasValue)
                            {
                                if (f.Type.Lookup.List.Class.Value == AssetTypeClass.Model)
                                {
                                    newFieldType.LookupObjectType = "TaxonomyType";
                                    newFieldType.LookupObjectID = 0;
                                }
                                else if (f.Type.Lookup.List.Class.Value == AssetTypeClass.Model)
                                {
                                    newFieldType.LookupObjectType = "ReferenceItemType";
                                    newFieldType.LookupObjectID = 0;
                                }
                                else
                                {
                                    throw new RestApiException(HttpStatusCode.BadRequest, "Field Type - list not specified", $"Lookup Field Type is incomplete as it does not have a valid class specified.");
                                }
                            }
                            else if (!f.Type.Lookup.List.Class.HasValue && f.Type.Lookup.List.Uid.HasValue)
                            {
                                var listAssetType = Company.Filter<AssetType>(i => i.uid == f.Type.Lookup.List.Uid.Value).SingleOrDefault();
                                if (listAssetType != null)
                                {
                                    newFieldType.LookupObjectType = listAssetType.Object.Replace("Type", "");
                                    newFieldType.LookupObjectID = listAssetType.ObjectID;
                                }
                                else
                                {
                                    throw new RestApiException(HttpStatusCode.NotFound, "List Asset Type not found", $"Asset Type not found for field [{f.Name}].");
                                }
                            }
                            else
                            {
                                throw new RestApiException(HttpStatusCode.BadRequest, "Field Type - list not specified", $"Lookup Field Type is incomplete as it does not have a List specified.");
                            }
                        }
                        else
                        {
                            throw new RestApiException(HttpStatusCode.BadRequest, "Field Type - list not specified", $"Lookup Field Type is incomplete as it does not have a List specified.");
                        }
                        if (f.Type.Lookup.Filter != null)
                        {
                            var filterFieldType = Company.Query<int>(@"select ID from FieldType where Object = @t and ObjectID = @tid and Name = @n", new { t = typeIdentifierInfoModel.Object, tid = typeIdentifierInfoModel.ObjectID, n = f.Type.Lookup.Filter.FieldTypeName }).FirstOrDefault();
                            if (filterFieldType <= 0)
                            {
                                throw new RestApiException(HttpStatusCode.NotFound, "Field Type not found", $"Field Type not found based on Name provided [{f.Type.Lookup.Filter.FieldTypeName}].");
                            }
                            var filterPredicate = Company.Query<int>(@"select ID from [Predicate] where Uid = @uid", new { uid = f.Type.Lookup.Filter.PredicateUid }).FirstOrDefault();
                            if (filterPredicate <= 0)
                            {
                                throw new RestApiException(HttpStatusCode.NotFound, "Field Type not found", $"Field Type not found based on Name provided [{f.Type.Lookup.Filter.FieldTypeName}].");
                            }
                            newFieldType.FilterFieldTypeID = filterFieldType;
                            newFieldType.FilterPredicateID = filterPredicate;
                            newFieldType.FilterPredicateDirection = f.Type.Lookup.Filter.UseDirection;
                        }
                        if (f.Type.Lookup.Format != null)
                        {
                            newFieldType.LookupDisplayFormat = f.Type.Lookup.Format.Display;
                            newFieldType.LookupEditFormat = f.Type.Lookup.Format.Edit;
                        }
                        newFieldType.IsDisplayable = f.Type.Lookup.IsDisplayable;
                        newFieldType.IsEditable = f.Type.Lookup.IsEditable;
                        newFieldType.IsListable = f.Type.Lookup.IsListable;
                        newFieldType.IsPartOfKey = f.Type.Lookup.IsPartOfKey;
                        newFieldType.IsPrimaryFilter = f.Type.Lookup.IsPrimaryFilter;
                        newFieldType.ShowIfEmpty = f.Type.Lookup.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.Lookup.SortOrder;
                        if (f.Type.Lookup.Validation != null)
                        {
                            newFieldType.IsRequired = f.Type.Lookup.Validation.IsRequired;
                        }
                    }
                    else if (f.Type.Number != null)
                    {
                        newFieldType.Type = DataType.Number.ToString();
                        newFieldType.ColumnOrder = f.Type.Number.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.Number.ColumnWidth;
                        if (f.Type.Number.DefaultValue.HasValue) newFieldType.DefaultValue = f.Type.Number.DefaultValue.Value.ToString();
                        if (f.Type.Number.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.Number.Description.Display;
                            newFieldType.FormDescription = f.Type.Number.Description.Form;
                        }
                        newFieldType.IsDisplayable = f.Type.Number.IsDisplayable;
                        newFieldType.IsEditable = f.Type.Number.IsEditable;
                        newFieldType.IsListable = f.Type.Number.IsListable;
                        newFieldType.IsPartOfKey = f.Type.Number.IsPartOfKey;
                        newFieldType.IsPrimaryFilter = f.Type.Number.IsPrimaryFilter;
                        newFieldType.ShowIfEmpty = f.Type.Number.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.Number.SortOrder;
                        newFieldType.Increment = f.Type.Number.Increment;
                        if (f.Type.Number.Validation != null)
                        {
                            newFieldType.IsRequired = f.Type.Number.Validation.IsRequired;
                            newFieldType.MaximumLength = f.Type.Number.Validation.MaximumLength;
                            newFieldType.MinimumLength = f.Type.Number.Validation.MinimumLength;
                        }
                    }
                    else if (f.Type.Relationship != null)
                    {
                        newFieldType.Type = DataType.Relationship.ToString();
                        newFieldType.ColumnOrder = f.Type.Relationship.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.Relationship.ColumnWidth;
                        if (f.Type.Relationship.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.Relationship.Description.Display;
                            newFieldType.FormDescription = f.Type.Relationship.Description.Form;
                        }
                        var relationshipType = Company.Query<int>(@"select ID from IntersectType where Uid = @uid", new { uid = f.Type.Relationship.IntersectTypeUid }).FirstOrDefault();
                        if (relationshipType <= 0)
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Relationship Type not found", $"Relationship Type not found based on Uid provided [{f.Type.ComputedRelationshipReferenceList.IntersectTypeUid}].");
                        }
                        newFieldType.LookupObjectType = "IntersectType";
                        newFieldType.LookupObjectID = relationshipType;
                        newFieldType.IsDisplayable = f.Type.Relationship.IsDisplayable;
                        newFieldType.IsEditable = f.Type.Relationship.IsEditable;
                        newFieldType.IsListable = f.Type.Relationship.IsListable;
                        newFieldType.IsPartOfKey = f.Type.Relationship.IsPartOfKey;
                        newFieldType.IsPrimaryFilter = f.Type.Relationship.IsPrimaryFilter;
                        newFieldType.ShowIfEmpty = f.Type.Relationship.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.Relationship.SortOrder;
                        if (f.Type.Relationship.Validation != null)
                        {
                            newFieldType.IsRequired = f.Type.Relationship.Validation.IsRequired;
                        }
                    }
                    else if (f.Type.Text != null)
                    {
                        newFieldType.Type = DataType.Text.ToString();
                        newFieldType.ColumnOrder = f.Type.Text.ColumnOrder;
                        newFieldType.ColumnWidth = f.Type.Text.ColumnWidth;
                        newFieldType.DefaultValue = f.Type.Text.DefaultValue;
                        if (f.Type.Text.Description != null)
                        {
                            newFieldType.DisplayDescription = f.Type.Text.Description.Display;
                            newFieldType.FormDescription = f.Type.Text.Description.Form;
                        }
                        newFieldType.IsDisplayable = f.Type.Text.IsDisplayable;
                        newFieldType.IsEditable = f.Type.Text.IsEditable;
                        newFieldType.IsListable = f.Type.Text.IsListable;
                        newFieldType.IsPartOfKey = f.Type.Text.IsPartOfKey;
                        newFieldType.IsPrimaryFilter = f.Type.Text.IsPrimaryFilter;
                        newFieldType.ShowIfEmpty = f.Type.Text.ShowIfEmpty;
                        newFieldType.SortOrder = f.Type.Text.SortOrder;
                        if (f.Type.Text.Validation != null)
                        {
                            newFieldType.IsRequired = f.Type.Text.Validation.IsRequired;
                            newFieldType.ValidationDescription = f.Type.Text.Validation.Message;
                            newFieldType.MaximumLength = f.Type.Text.Validation.MaximumLength;
                            newFieldType.MinimumLength = f.Type.Text.Validation.MinimumLength;
                            newFieldType.Pattern = f.Type.Text.Validation.Pattern;
                        }
                    }
                    else
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, "No valid type defined", $"You have not included a valid type for the field type [{f.Name}].");
                    }

                    var currentFieldType = currentFieldTypes.SingleOrDefault(c => c.Name == f.Name);
                    if (currentFieldType == null)
                    {
                        newFieldTypes.Add(newFieldType);
                    }
                    else
                    {
                        if (!allowedConversions.Any(i => i.FromType == currentFieldType.Type && i.ToType == newFieldType.Type) && (currentFieldType.Type != newFieldType.Type))
                        {
                            throw new RestApiException(HttpStatusCode.BadRequest, "Field conversion error", $"You may not convert field {newFieldType.Name} from a {currentFieldType.Type} to a {newFieldType.Type}.");
                        }

                        currentFieldType.AllowAllLabel = newFieldType.AllowAllLabel;
                        currentFieldType.AllowAllValue = newFieldType.AllowAllValue;
                        currentFieldType.AllowMultipleValues = newFieldType.AllowMultipleValues;
                        currentFieldType.Category = newFieldType.Category;
                        currentFieldType.ColumnOrder = newFieldType.ColumnOrder;
                        currentFieldType.ColumnWidth = newFieldType.ColumnWidth;
                        currentFieldType.DefaultValue = newFieldType.DefaultValue;
                        currentFieldType.DisplayDescription = newFieldType.DisplayDescription;
                        if (currentFieldType.FieldTypeLookup != null)
                        {
                            if (newFieldType.FieldTypeLookup != null)
                            {
                                currentFieldType.FieldTypeLookup.Definition = newFieldType.FieldTypeLookup.Definition;
                                currentFieldType.FieldTypeLookup.HideFilter = newFieldType.FieldTypeLookup.HideFilter;
                                currentFieldType.FieldTypeLookup.HideFooter = newFieldType.FieldTypeLookup.HideFooter;
                                currentFieldType.FieldTypeLookup.HideHeader = newFieldType.FieldTypeLookup.HideHeader;
                                currentFieldType.FieldTypeLookup.LookupType = newFieldType.FieldTypeLookup.LookupType;
                            }
                            else
                            {
                                currentFieldType.FieldTypeLookup = null;
                            }
                        }
                        else
                        {
                            if (newFieldType.FieldTypeLookup != null)
                            {
                                currentFieldType.FieldTypeLookup = new FieldTypeLookup
                                {
                                    Definition = newFieldType.FieldTypeLookup.Definition,
                                    HideFilter = newFieldType.FieldTypeLookup.HideFilter,
                                    HideFooter = newFieldType.FieldTypeLookup.HideFooter,
                                    HideHeader = newFieldType.FieldTypeLookup.HideHeader,
                                    LookupType = newFieldType.FieldTypeLookup.LookupType
                                };
                            }
                        }
                        currentFieldType.FilterFieldTypeID = newFieldType.FilterFieldTypeID;
                        currentFieldType.FilterPredicateDirection = newFieldType.FilterPredicateDirection;
                        currentFieldType.FilterPredicateID = newFieldType.FilterPredicateID;
                        currentFieldType.FormDescription = newFieldType.FormDescription;
                        currentFieldType.FriendlyName = newFieldType.FriendlyName;
                        currentFieldType.Increment = newFieldType.Increment;
                        currentFieldType.IsDisplayable = newFieldType.IsDisplayable;
                        currentFieldType.IsEditable = newFieldType.IsEditable;
                        currentFieldType.IsListable = newFieldType.IsListable;
                        currentFieldType.IsPartOfKey = newFieldType.IsPartOfKey;
                        currentFieldType.IsPrimaryFilter = newFieldType.IsPrimaryFilter;
                        currentFieldType.IsRequired = newFieldType.IsRequired;
                        currentFieldType.Length = newFieldType.Length;
                        currentFieldType.LookupDisplayFormat = newFieldType.LookupDisplayFormat;
                        currentFieldType.LookupEditFormat = newFieldType.LookupEditFormat;
                        currentFieldType.LookupObjectFieldTypeID = newFieldType.LookupObjectFieldTypeID;
                        currentFieldType.LookupObjectID = newFieldType.LookupObjectID;
                        currentFieldType.LookupObjectType = newFieldType.LookupObjectType;
                        currentFieldType.MaximumLength = newFieldType.MaximumLength;
                        currentFieldType.MinimumLength = newFieldType.MinimumLength;
                        currentFieldType.ParentFieldTypeID = newFieldType.ParentFieldTypeID;
                        currentFieldType.Pattern = newFieldType.Pattern;
                        currentFieldType.Precision = newFieldType.Precision;
                        currentFieldType.ShowIfEmpty = newFieldType.ShowIfEmpty;
                        currentFieldType.SortOrder = newFieldType.SortOrder;
                        currentFieldType.Type = newFieldType.Type;
                        currentFieldType.ValidationDescription = newFieldType.ValidationDescription;

                        fieldTypeNamesToDelete.Add(f.Name);
                    }

                });

                if (model.Action == FieldTypesApiEditAction.Merge)
                {
                    fieldTypeNamesToDelete.ForEach(d =>
                    {
                        newFieldTypes.RemoveAll(i => i.Name == d);
                    });
                    Company.FieldTypes.AddRange(newFieldTypes);
                }
                else  // Replace
                {
                    Company.Execute("delete FieldType where Object = @t and ObjectID = @tid", new { t = typeIdentifierInfoModel.Object, tid = typeIdentifierInfoModel.ObjectID });
                    Company.FieldTypes.AddRange(newFieldTypes);
                }
                Company.SaveChanges();

                #endregion

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ApiStatusResponse { Message = "Fields successfully updated.", Success = true, Uid = typeIdentifierInfoModel.Uid })));
            }
            catch (RestApiException ex)
            {
                errorMessage = ex.GetFullExceptionData(false);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(ex.Status, errorMessage)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(HttpStatusCode.InternalServerError, errorMessage)));
            }

        }

        /// <summary>
        /// Removes field types contained within your environment.
        /// </summary>
        /// <remarks>
        /// You may only provide one of the following: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.
        /// </remarks>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpDelete,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteFieldTypesAsync(FieldTypesApiDeleteModel model)
        {
            var prefix = "Fields.DeleteFieldTypesAsync => ";
            var errorMessage = "";

            try
            {
                IEnumerable<TypeIdentifierInfoModel> typeIdentifierInfoModels = null;
                TypeIdentifierInfoModel typeIdentifierInfoModel = null;

                #region Validation

                if (model == null)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "No model found", "You did not provide a valid model. Please check your request and try again.");
                }

                if (!model.ActionTypeUid.HasValue && !model.AssetTypeUid.HasValue && !model.RelationshipTypeUid.HasValue)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "No Uid found", "You must provide only one of the three Uid properties: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.");
                }

                if (model.ActionTypeUid.HasValue)
                {
                    typeIdentifierInfoModels = await Company.QueryAsync<TypeIdentifierInfoModel>("select ID, Uid, 'IssueType' as Object, ID as ObjectID from IssueType where Uid = @uid", new { uid = model.ActionTypeUid.Value });
                    typeIdentifierInfoModel = typeIdentifierInfoModels.SingleOrDefault();
                    if (typeIdentifierInfoModel == null)
                    {
                        throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Action Type not found based on Uid provided [{model.ActionTypeUid}].");
                    }
                }

                if (model.AssetTypeUid.HasValue)
                {
                    if (model.ActionTypeUid.HasValue)
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an AssetTypeUid since you have already provided an ActionTypeUid.");
                    }
                    else
                    {
                        typeIdentifierInfoModels = await Company.QueryAsync<TypeIdentifierInfoModel>("select ID, Uid, Object, ObjectID from AssetType where Uid = @uid", new { uid = model.AssetTypeUid.Value });
                        typeIdentifierInfoModel = typeIdentifierInfoModels.SingleOrDefault();
                        if (typeIdentifierInfoModel == null)
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Asset Type not found based on Uid provided [{model.ActionTypeUid}].");
                        }
                    }
                }

                if (model.RelationshipTypeUid.HasValue)
                {
                    if (model.ActionTypeUid.HasValue)
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an ActionTypeUid.");
                    }
                    else if (model.AssetTypeUid.HasValue)
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an AssetTypeUid.");
                    }
                    else
                    {
                        typeIdentifierInfoModels = await Company.QueryAsync<TypeIdentifierInfoModel>("select ID, Uid, 'IntersectType' as Object, ID as ObjectID from IntersectType where Uid = @uid", new { uid = model.RelationshipTypeUid.Value });
                        typeIdentifierInfoModel = typeIdentifierInfoModels.SingleOrDefault();
                        if (typeIdentifierInfoModel == null)
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Relationship Type not found based on Uid provided [{model.RelationshipTypeUid}].");
                        }
                    }
                }

                #region Security check

                bool hasPermissions = false;

                if (Company.CurrentResourceIsAdmin)
                {
                    hasPermissions = true;
                }
                else
                {
                    var typePermissions = Company.GetTypePermissions(typeIdentifierInfoModel.Object, typeIdentifierInfoModel.ObjectID);
                    if (typePermissions != null)
                    {
                        hasPermissions = typePermissions.Any(i => i.ID == Permission.DeleteAsset);
                    }
                }

                if (!hasPermissions)
                {
                    throw new RestApiException(HttpStatusCode.Unauthorized, "Not authorized", "You do not have permissions to remove fields on this type.");
                }

                #endregion

                var anyExistingItems = false;
                switch (typeIdentifierInfoModel.Object)
                {
                    case "IntersectType":
                        anyExistingItems = Company.Any<Intersect>(i => i.IntersectTypeID == typeIdentifierInfoModel.ID);
                        break;
                    case "IssueType":
                        anyExistingItems = Company.Any<Issue>(i => i.IssueTypeID == typeIdentifierInfoModel.ID);
                        break;
                    default:
                        anyExistingItems = Company.Any<Asset>(i => i.AssetTypeID == typeIdentifierInfoModel.ID);
                        break;
                }

                var currentFieldTypes = Company.Filter<FieldType>(f => f.Object == typeIdentifierInfoModel.Object && f.ObjectID == typeIdentifierInfoModel.ObjectID, i => i.FieldTypeLookup).ToList();
                var fieldNamesToDelete = model.Fields.Select(i => i.Name).ToList();

                var keyFieldsWillBeDeleted = currentFieldTypes.Any(d => d.IsPartOfKey == true && fieldNamesToDelete.Contains(d.Name));

                if (anyExistingItems && keyFieldsWillBeDeleted)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Existing items in system", $"You may not remove key fields as there are existing items in your environment. You may not perform a Delete action until those items are removed, or you alter the key fields defined on this type.");
                }

                var anyInvalidFields = fieldNamesToDelete.Any(f => !currentFieldTypes.Any(c => c.Name == f));
                if (anyInvalidFields)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Invalid fields", $"You are attempting to remove one or more fields that do not exist on this type.");
                }

                #endregion

                #region Validation done, time to do some work

                var fieldsRemoved = false;
                currentFieldTypes.ForEach(c =>
                {
                    if (fieldNamesToDelete.Contains(c.Name))
                    {
                        Company.FieldTypes.Remove(c);
                        fieldsRemoved = true;
                    }
                });

                if (fieldsRemoved)
                {
                    Company.SaveChanges();
                }

                #endregion

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ApiStatusResponse { Message = "Fields successfully removed.", Success = true, Uid = typeIdentifierInfoModel.Uid })));
            }
            catch (RestApiException ex)
            {
                errorMessage = ex.GetFullExceptionData(false);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(ex.Status, errorMessage)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(HttpStatusCode.InternalServerError, errorMessage)));
            }

        }
    }
}
