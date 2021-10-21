using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.core.Models;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;
using d360.model.helpers.filters;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace d360.model.DataAccessLayer
{
    public class FieldsRepository : BaseRepository, IFieldsRepository
    {
        internal ICompanyContext Company;
        internal IQueueSource QueueSource;
        internal IStorageProvider StorageProvider;
        public FieldsRepository(ICompanyContext companyContext, IQueueSource queueSource, IStorageProvider storageProvider)
            : base(companyContext)
        {
            this.Company = companyContext;
            this.QueueSource = queueSource;
            this.StorageProvider = storageProvider;
        }

        public async Task<Tuple<FieldTypesApiViewModel, WorkHttpStatus>> GetFieldTypes(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            Guid? actionTypeUid = null;
            Guid? assetTypeUid = null;
            Guid? relationshipTypeUid = null;
            int pageNumber = 1;
            int pageSize = 250;

            var whereClause = "";
            string orderByClause = " order by FT.Object, FT.ObjectID, FT.Name ";
            #region Parameter Checking

            var dbArgs = new DynamicParameters();

            var parameters = queryParams.ToList();

            string obj = null;
            int? objID = null;

            WorkHttpStatus workHttpStatus = new WorkHttpStatus(HttpStatusCode.OK, "", "");
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
                        workHttpStatus = new WorkHttpStatus(HttpStatusCode.NotFound, "Type not found", $"Action Type not found based on Uid provided [{actionTypeUid.ToString()}].");
                    }
                }
            }
            if (parameters.Any(q => q.Key.ToLower() == "assettypeuid"))
            {
                if (actionTypeUid.HasValue)
                {
                    workHttpStatus = new WorkHttpStatus(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an AssetTypeUid since you have already provided an ActionTypeUid.");
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
                            workHttpStatus = new WorkHttpStatus(HttpStatusCode.NotFound, "Type not found", $"Asset Type not found based on Uid provided [{assetTypeUid.ToString()}].");
                        }
                    }
                    else
                    {
                        workHttpStatus = new WorkHttpStatus(HttpStatusCode.NotFound, "Type not found", $"Invalid Asset Type Uid provided [{assetTypeUidString}].");
                    }
                }
            }
            if (parameters.Any(q => q.Key.ToLower() == "relationshiptypeuid"))
            {
                if (actionTypeUid.HasValue)
                {
                    workHttpStatus = new WorkHttpStatus(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an ActionTypeUid.");
                }
                else if (assetTypeUid.HasValue)
                {
                    workHttpStatus = new WorkHttpStatus(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an AssetTypeUid.");
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
                            workHttpStatus = new WorkHttpStatus(HttpStatusCode.NotFound, "Type not found", $"Relationship Type not found based on Uid provided [{relationshipTypeUid.ToString()}].");
                        }
                    }
                }
            }
            if (actionTypeUid.HasValue || assetTypeUid.HasValue || relationshipTypeUid.HasValue)
            {
                orderByClause = " order by FT.ColumnOrder, FT.Name ";
            }

            if (workHttpStatus.StatusCode != HttpStatusCode.OK)
                return new Tuple<FieldTypesApiViewModel, WorkHttpStatus>(new FieldTypesApiViewModel(), workHttpStatus);

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
                case when FT.Type = 'Boolean' then FT.IsRequired else null end as 'Type.Boolean.Validation.IsRequired', 
                case when FT.Type = 'Boolean' then FT.SearchAddToResult else null end as 'Type.Boolean.Search.AddToResult', 
                case when FT.Type = 'Boolean' then FT.SearchPrefix else null end as 'Type.Boolean.Search.Prefix', 
                case when FT.Type = 'Boolean' then FT.SearchSuffix else null end as 'Type.Boolean.Search.Suffix', 
                case when FT.Type = 'Boolean' then FT.SearchDisplayOrder else null end as 'Type.Boolean.Search.DisplayOrder', 
                case when FT.Type = 'Boolean' then FT.DisplayInColumn else null end as 'Type.Boolean.DisplayInColumn', 


		        case when FT.Type = 'OwnershipLookup' then FT.ColumnOrder else null end as 'Type.ComputedOwnershipLookup.ColumnOrder',
		        case when FT.Type = 'OwnershipLookup' then FT.DisplayDescription else null end as 'Type.ComputedOwnershipLookup.Description.Display',
		        case when FT.Type = 'OwnershipLookup' then FTL.HideHeader else null end as 'Type.ComputedOwnershipLookup.HideHeader', 
		        case when FT.Type = 'OwnershipLookup' then FTL.HideFooter else null end as 'Type.ComputedOwnershipLookup.HideFooter', 
		        case when FT.Type = 'OwnershipLookup' then FTL.HideFilter else null end as 'Type.ComputedOwnershipLookup.HideFilter', 
                case when FT.Type = 'OwnershipLookup' then try_cast(JSON_VALUE(FTL.Definition, '$.DisplayAsList') as bit) else null end as 'Type.ComputedOwnershipLookup.Definition.DisplayAsList',
                case when FT.Type = 'OwnershipLookup' then try_cast(JSON_VALUE(FTL.Definition, '$.DisplayAssignmentSource') as bit) else null end as 'Type.ComputedOwnershipLookup.Definition.DisplayAssignmentSource',
		        case when FT.Type = 'OwnershipLookup' then try_cast(JSON_VALUE(FTL.Definition, '$.ExpandGroupMembership') as bit) else null end as 'Type.ComputedOwnershipLookup.Definition.ExpandGroupMembership',
		        case when FT.Type = 'OwnershipLookup' then (select uid FROM ResponsibilityType where id = try_cast(JSON_VALUE(FTL.Definition, '$.ResponsibilityType') as int)) else null end as 'Type.ComputedOwnershipLookup.Definition.ResponsibilityTypeUid',
		        case when FT.Type = 'OwnershipLookup' then FT.IsDisplayable else null end as 'Type.ComputedOwnershipLookup.IsDisplayable',
		        case when FT.Type = 'OwnershipLookup' then FT.ShowIfEmpty else null end as 'Type.ComputedOwnershipLookup.ShowIfEmpty',
		        case when FT.Type = 'OwnershipLookup' then FT.IsListable else null end as 'Type.ComputedOwnershipLookup.IsListable',
		        case when FT.Type = 'OwnershipLookup' then FT.ColumnWidth else null end as 'Type.ComputedOwnershipLookup.ColumnWidth',
		        case when FT.Type = 'OwnershipLookup' then FT.SortOrder else null end as 'Type.ComputedOwnershipLookup.SortOrder',
                case when FT.Type = 'OwnershipLookup' then FT.DisplayInColumn else null end as 'Type.ComputedOwnershipLookup.DisplayInColumn', 

		        case when FT.Type = 'FieldFromRelationship' then FT.ColumnOrder else null end as 'Type.ComputedRelationshipField.ColumnOrder',
		        case when FT.Type = 'FieldFromRelationship' then FT.ColumnWidth else null end as 'Type.ComputedRelationshipField.ColumnWidth',
		        case when FT.Type = 'FieldFromRelationship' then FT.SortOrder else null end as 'Type.ComputedRelationshipField.SortOrder',
		        case when FT.Type = 'FieldFromRelationship' then FT.DisplayDescription else null end as 'Type.ComputedRelationshipField.Description.Display',
		        case when FT.Type = 'FieldFromRelationship' then IT.Uid else null end as 'Type.ComputedRelationshipField.IntersectTypeUid',
		        case when FT.Type = 'FieldFromRelationship' then LFT.Name else null end as 'Type.ComputedRelationshipField.FieldTypeName',
		        case when FT.Type = 'FieldFromRelationship' then FT.IsDisplayable else null end as 'Type.ComputedRelationshipField.IsDisplayable',
		        case when FT.Type = 'FieldFromRelationship' then FT.IsListable else null end as 'Type.ComputedRelationshipField.IsListable',
		        case when FT.Type = 'FieldFromRelationship' then FT.ShowIfEmpty else null end as 'Type.ComputedRelationshipField.ShowIfEmpty',
                case when FT.Type = 'FieldFromRelationship' then FT.SearchAddToResult else null end as 'Type.ComputedRelationshipField.Search.AddToResult', 
                case when FT.Type = 'FieldFromRelationship' then FT.SearchPrefix else null end as 'Type.ComputedRelationshipField.Search.Prefix', 
                case when FT.Type = 'FieldFromRelationship' then FT.SearchSuffix else null end as 'Type.ComputedRelationshipField.Search.Suffix', 
                case when FT.Type = 'FieldFromRelationship' then FT.SearchDisplayOrder else null end as 'Type.ComputedRelationshipField.Search.DisplayOrder', 
                case when FT.Type = 'FieldFromRelationship' then FT.DisplayInColumn else null end as 'Type.ComputedRelationshipField.DisplayInColumn', 

		        case when FT.Type = 'ComplexRelationLookup' then FT.ColumnOrder else null end as 'Type.ComputedRelationshipLookup.ColumnOrder',
		        case when FT.Type = 'ComplexRelationLookup' then FT.DisplayDescription else null end as 'Type.ComputedRelationshipLookup.Description.Display',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.HideHeader else null end as 'Type.ComputedRelationshipLookup.HideHeader',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.HideFooter else null end as 'Type.ComputedRelationshipLookup.HideFooter',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.HideFilter else null end as 'Type.ComputedRelationshipLookup.HideFilter',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.LookupType else null end as 'Type.ComputedRelationshipLookup.LookupType',

		        JSON_QUERY(case when FT.Type = 'ComplexRelationLookup' then (
		        select	DR.IntersectTypeUid,
				        DR.AssetTypeUid,
				        DR.RelationType,
				        DR.Direction
		        from	OPENJSON(FTL.Definition) with (Relations nvarchar(max) as json) D
				        outer apply OPENJSON(D.Relations) with (IntersectTypeUid uniqueidentifier, AssetTypeUid uniqueidentifier, RelationType int, Direction int) DR
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
				        DF.Width,
                        DF.RelationIndex
		        from	OPENJSON(FTL.Definition) with (Fields nvarchar(max) as json) D
				        outer apply OPENJSON(D.Fields) with (AssetTypeUid uniqueidentifier, FieldTypeID int, FieldTypeName nvarchar(250), [Filter] nvarchar(500), OverrideDisplayName nvarchar(250), DisplayOrder int, SortOrder int, Show bit, Width int, RelationIndex int) DF
				        left join AssetType AST on AST.Uid = DF.AssetTypeUid
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
                case when FT.Type = 'RefListRelationship' then coalesce(JSON_VALUE(FT.Definition,'$.DisplayRefListDescription'),'true') else null end as 'Type.ComputedRelationshipReferenceList.DisplayRefListDescription',

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
                case when FT.Type = 'Date' then FT.SearchAddToResult else null end as 'Type.Date.Search.AddToResult', 
                case when FT.Type = 'Date' then FT.SearchPrefix else null end as 'Type.Date.Search.Prefix', 
                case when FT.Type = 'Date' then FT.SearchSuffix else null end as 'Type.Date.Search.Suffix', 
                case when FT.Type = 'Date' then FT.SearchDisplayOrder else null end as 'Type.Date.Search.DisplayOrder', 
                case when FT.Type = 'Date' then FT.DisplayInColumn else null end as 'Type.Date.DisplayInColumn', 

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
                case when FT.Type = 'DateTime' then FT.SearchAddToResult else null end as 'Type.DateTime.Search.AddToResult', 
                case when FT.Type = 'DateTime' then FT.SearchPrefix else null end as 'Type.DateTime.Search.Prefix', 
                case when FT.Type = 'DateTime' then FT.SearchSuffix else null end as 'Type.DateTime.Search.Suffix', 
                case when FT.Type = 'DateTime' then FT.SearchDisplayOrder else null end as 'Type.DateTime.Search.DisplayOrder', 
                case when FT.Type = 'DateTime' then FT.DisplayInColumn else null end as 'Type.DateTime.DisplayInColumn', 

		        case when FT.Type = 'Decimal' then FT.ColumnOrder else null end as 'Type.Decimal.ColumnOrder',
		        case when FT.Type = 'Decimal' then FT.ColumnWidth else null end as 'Type.Decimal.ColumnWidth',
		        case when FT.Type = 'Decimal' then FT.SortOrder else null end as 'Type.Decimal.SortOrder',
		        case when FT.Type = 'Decimal' then TRY_CAST(FT.DefaultValue as decimal) else null end as 'Type.Decimal.DefaultValue',
		        case when FT.Type = 'Decimal' then FT.DisplayDescription else null end as 'Type.Decimal.Description.Display',
		        case when FT.Type = 'Decimal' then FT.FormDescription else null end as 'Type.Decimal.Description.Form',
		        case when FT.Type = 'Decimal' then FT.Increment else null end as 'Type.Decimal.Increment',
		        case when FT.Type = 'Decimal' then FT.MinimumLength else null end as 'Type.Decimal.Validation.MinimumValue',
		        case when FT.Type = 'Decimal' then FT.MaximumLength else null end as 'Type.Decimal.Validation.MaximumValue',
		        case when FT.Type = 'Decimal' then FT.[Precision] else null end as 'Type.Decimal.Validation.Precision',
		        case when FT.Type = 'Decimal' then FT.IsRequired else null end as 'Type.Decimal.Validation.IsRequired',
		        case when FT.Type = 'Decimal' then FT.IsDisplayable else null end as 'Type.Decimal.IsDisplayable',
		        case when FT.Type = 'Decimal' then FT.IsEditable else null end as 'Type.Decimal.IsEditable',
		        case when FT.Type = 'Decimal' then FT.IsListable else null end as 'Type.Decimal.IsListable',
		        case when FT.Type = 'Decimal' then FT.IsPartOfKey else null end as 'Type.Decimal.IsPartOfKey',
		        case when FT.Type = 'Decimal' then FT.IsPrimaryFilter else null end as 'Type.Decimal.IsPrimaryFilter',
		        case when FT.Type = 'Decimal' then FT.ShowIfEmpty else null end as 'Type.Decimal.ShowIfEmpty',
                case when FT.Type = 'Decimal' then FT.SearchAddToResult else null end as 'Type.Decimal.Search.AddToResult', 
                case when FT.Type = 'Decimal' then FT.SearchPrefix else null end as 'Type.Decimal.Search.Prefix', 
                case when FT.Type = 'Decimal' then FT.SearchSuffix else null end as 'Type.Decimal.Search.Suffix', 
                case when FT.Type = 'Decimal' then FT.SearchDisplayOrder else null end as 'Type.Decimal.Search.DisplayOrder', 
                case when FT.Type = 'Decimal' then FT.DisplayInColumn else null end as 'Type.Decimal.DisplayInColumn', 

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
                case when FT.Type = 'Html' then FT.DisplayInColumn else null end as 'Type.Html.DisplayInColumn', 

		        case when FT.Type = 'Json' then FT.ColumnOrder else null end as 'Type.Json.ColumnOrder',
		        case when FT.Type = 'Json' then FT.DisplayDescription else null end as 'Type.Json.Description.Display',
		        case when FT.Type = 'Json' then FT.IsDisplayable else null end as 'Type.Json.IsDisplayable',
		        case when FT.Type = 'Json' then FT.IsEditable else null end as 'Type.Json.IsEditable',
                case when FT.Type = 'Json' then FT.IsRequired else null end as 'Type.Json.Validation.IsRequired',
		        case when FT.Type = 'Json' then FT.ShowIfEmpty else null end as 'Type.Json.ShowIfEmpty',

		        case when FT.Type = 'JsonElement' then FT.ColumnOrder else null end as 'Type.JsonElement.ColumnOrder',
		        case when FT.Type = 'JsonElement' then FT.DisplayDescription else null end as 'Type.JsonElement.Description.Display',
		        case when FT.Type = 'JsonElement' then FT.IsDisplayable else null end as 'Type.JsonElement.IsDisplayable',
				case when FT.Type = 'JsonElement' then FT.ShowIfEmpty else null end as 'Type.JsonElement.ShowIfEmpty',
				case when FT.Type = 'JsonElement' then FT.IsListable else null end as 'Type.JsonElement.IsListable',
                case when FT.Type = 'JsonElement' then (select Name from FieldType where ID = JSON_VALUE(FT.Definition,'$.FieldTypeID')) else null end as 'Type.JsonElement.JsonAttribute.FieldName',
				case when FT.Type = 'JsonElement' then JSON_VALUE(FT.Definition,'$.Path') else null end as 'Type.JsonElement.JsonAttribute.Path',
				case when FT.Type = 'JsonElement' then JSON_VALUE(FT.Definition,'$.DataType') else null end as 'Type.JsonElement.JsonAttribute.DataType',

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
		        case when FT.Type = 'Link' then FT.IsPartOfKey else null end as 'Type.Link.IsPartOfKey',
		        case when FT.Type = 'Link' then FT.IsPrimaryFilter else null end as 'Type.Link.IsPrimaryFilter',
                case when FT.Type = 'Link' then FT.SearchAddToResult else null end as 'Type.Link.Search.AddToResult', 
                case when FT.Type = 'Link' then FT.SearchPrefix else null end as 'Type.Link.Search.Prefix', 
                case when FT.Type = 'Link' then FT.SearchSuffix else null end as 'Type.Link.Search.Suffix', 
                case when FT.Type = 'Link' then FT.SearchDisplayOrder else null end as 'Type.Link.Search.DisplayOrder', 
                case when FT.Type = 'Link' then FT.DisplayInColumn else null end as 'Type.Link.DisplayInColumn', 

		        case when FT.Type = 'Lookup' then FT.ColumnOrder else null end as 'Type.Lookup.ColumnOrder',
		        case when FT.Type = 'Lookup' then FT.ColumnWidth else null end as 'Type.Lookup.ColumnWidth',
		        case when FT.Type = 'Lookup' then FT.SortOrder else null end as 'Type.Lookup.SortOrder',
		        case when FT.Type = 'Lookup' then COALESCE(TRY_CONVERT(UNIQUEIDENTIFIER, FT.DefaultValue),DFA.[Uid]) else null end as 'Type.Lookup.DefaultValue',
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
                case when FT.Type = 'Lookup' then (select Name from FieldType where ID = FT.ParentFieldTypeID) else null end as 'Type.Lookup.ParentFieldTypeName',
		        case when FT.Type = 'Lookup' then FT.IsDisplayable else null end as 'Type.Lookup.IsDisplayable',
		        case when FT.Type = 'Lookup' then FT.IsEditable else null end as 'Type.Lookup.IsEditable',
		        case when FT.Type = 'Lookup' then FT.IsListable else null end as 'Type.Lookup.IsListable',
		        case when FT.Type = 'Lookup' then FT.IsPartOfKey else null end as 'Type.Lookup.IsPartOfKey',
		        case when FT.Type = 'Lookup' then FT.IsPrimaryFilter else null end as 'Type.Lookup.IsPrimaryFilter',
		        case when FT.Type = 'Lookup' then FT.ShowIfEmpty else null end as 'Type.Lookup.ShowIfEmpty',
                case when FT.Type = 'Lookup' then FT.SearchAddToResult else null end as 'Type.Lookup.Search.AddToResult', 
                case when FT.Type = 'Lookup' then FT.SearchPrefix else null end as 'Type.Lookup.Search.Prefix', 
                case when FT.Type = 'Lookup' then FT.SearchSuffix else null end as 'Type.Lookup.Search.Suffix', 
                case when FT.Type = 'Lookup' then FT.SearchDisplayOrder else null end as 'Type.Lookup.Search.DisplayOrder', 
                case when FT.Type = 'Lookup' then FT.DisplayInColumn else null end as 'Type.Lookup.DisplayInColumn', 

		        case when FT.Type = 'Number' then FT.ColumnOrder else null end as 'Type.Number.ColumnOrder',
		        case when FT.Type = 'Number' then FT.ColumnWidth else null end as 'Type.Number.ColumnWidth',
		        case when FT.Type = 'Number' then FT.SortOrder else null end as 'Type.Number.SortOrder',
		        case when FT.Type = 'Number' then TRY_CAST(FT.DefaultValue as int) else null end as 'Type.Number.DefaultValue',
		        case when FT.Type = 'Number' then FT.DisplayDescription else null end as 'Type.Number.Description.Display',
		        case when FT.Type = 'Number' then FT.FormDescription else null end as 'Type.Number.Description.Form',
		        case when FT.Type = 'Number' then FT.Increment else null end as 'Type.Number.Increment',
		        case when FT.Type = 'Number' then FT.MinimumLength else null end as 'Type.Number.Validation.MinimumValue',
		        case when FT.Type = 'Number' then FT.MaximumLength else null end as 'Type.Number.Validation.MaximumValue',
		        case when FT.Type = 'Number' then FT.IsRequired else null end as 'Type.Number.Validation.IsRequired',
		        case when FT.Type = 'Number' then FT.IsDisplayable else null end as 'Type.Number.IsDisplayable',
		        case when FT.Type = 'Number' then FT.IsEditable else null end as 'Type.Number.IsEditable',
		        case when FT.Type = 'Number' then FT.IsListable else null end as 'Type.Number.IsListable',
		        case when FT.Type = 'Number' then FT.IsPartOfKey else null end as 'Type.Number.IsPartOfKey',
		        case when FT.Type = 'Number' then FT.IsPrimaryFilter else null end as 'Type.Number.IsPrimaryFilter',
		        case when FT.Type = 'Number' then FT.ShowIfEmpty else null end as 'Type.Number.ShowIfEmpty',
                case when FT.Type = 'Number' then FT.SearchAddToResult else null end as 'Type.Number.Search.AddToResult', 
                case when FT.Type = 'Number' then FT.SearchPrefix else null end as 'Type.Number.Search.Prefix', 
                case when FT.Type = 'Number' then FT.SearchSuffix else null end as 'Type.Number.Search.Suffix', 
                case when FT.Type = 'Number' then FT.SearchDisplayOrder else null end as 'Type.Number.Search.DisplayOrder', 
                case when FT.Type = 'Number' then FT.DisplayInColumn else null end as 'Type.Number.DisplayInColumn', 

		        case when FT.Type = 'Path' then FT.ColumnOrder else null end as 'Type.Path.ColumnOrder',
		        case when FT.Type = 'Path' then FT.ColumnWidth else null end as 'Type.Path.ColumnWidth',
		        case when FT.Type = 'Path' then FT.SortOrder else null end as 'Type.Path.SortOrder',
		        case when FT.Type = 'Path' then FT.DisplayDescription else null end as 'Type.Path.Description.Display',
		        case when FT.Type = 'Path' then FT.IsDisplayable else null end as 'Type.Path.IsDisplayable',
		        case when FT.Type = 'Path' then FT.IsListable else null end as 'Type.Path.IsListable',
                case when FT.Type = 'Path' then FT.DisplayInColumn else null end as 'Type.Path.DisplayInColumn', 

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
                case when FT.Type = 'Relationship' then FT.SearchAddToResult else null end as 'Type.Relationship.Search.AddToResult', 
                case when FT.Type = 'Relationship' then FT.SearchPrefix else null end as 'Type.Relationship.Search.Prefix', 
                case when FT.Type = 'Relationship' then FT.SearchSuffix else null end as 'Type.Relationship.Search.Suffix', 
                case when FT.Type = 'Relationship' then FT.SearchDisplayOrder else null end as 'Type.Relationship.Search.DisplayOrder', 
                case when FT.Type = 'Relationship' then FT.DisplayInColumn else null end as 'Type.Relationship.DisplayInColumn', 

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
		        case when FT.Type = 'Text' then FT.ShowIfEmpty else null end as 'Type.Text.ShowIfEmpty',
                case when FT.Type = 'Text' then FT.SearchAddToResult else null end as 'Type.Text.Search.AddToResult', 
                case when FT.Type = 'Text' then FT.SearchPrefix else null end as 'Type.Text.Search.Prefix', 
                case when FT.Type = 'Text' then FT.SearchSuffix else null end as 'Type.Text.Search.Suffix', 
                case when FT.Type = 'Text' then FT.SearchDisplayOrder else null end as 'Type.Text.Search.DisplayOrder', 
                case when FT.Type = 'Text' then FT.DisplayInColumn else null end as 'Type.Text.DisplayInColumn', 

		        case when FT.Type = 'Tag' then FT.ColumnOrder else null end as 'Type.Tag.ColumnOrder',
		        case when FT.Type = 'Tag' then FT.ColumnWidth else null end as 'Type.Tag.ColumnWidth',
		        case when FT.Type = 'Tag' then FT.SortOrder else null end as 'Type.Tag.SortOrder',
		        case when FT.Type = 'Tag' then FT.DisplayDescription else null end as 'Type.Tag.Description.Display',
		        case when FT.Type = 'Tag' then FT.FormDescription else null end as 'Type.Tag.Description.Form',
		        case when FT.Type = 'Tag' then FT.IsRequired else null end as 'Type.Tag.Validation.IsRequired',
		        case when FT.Type = 'Tag' then FT.IsDisplayable else null end as 'Type.Tag.IsDisplayable',
		        case when FT.Type = 'Tag' then FT.IsEditable else null end as 'Type.Tag.IsEditable',
		        case when FT.Type = 'Tag' then FT.IsListable else null end as 'Type.Tag.IsListable',
		        case when FT.Type = 'Tag' then FT.IsPartOfKey else null end as 'Type.Tag.IsPartOfKey',
		        case when FT.Type = 'Tag' then FT.IsPrimaryFilter else null end as 'Type.Tag.IsPrimaryFilter',
		        case when FT.Type = 'Tag' then FT.ShowIfEmpty else null end as 'Type.Tag.ShowIfEmpty',

                case when FT.Type = 'Score' then FT.ScoreType else null end as 'Type.Score.ScoreType',
                case when FT.Type = 'Score' then FT.ColumnOrder else null end as 'Type.Score.ColumnOrder',
                case when FT.Type = 'Score' then FT.ColumnWidth else null end as 'Type.Score.ColumnWidth',
                case when FT.Type = 'Score' then FT.SortOrder else null end as 'Type.Score.SortOrder',
                case when FT.Type = 'Score' then FT.DisplayDescription else null end as 'Type.Score.Description.Display',
                case when FT.Type = 'Score' then FT.IsDisplayable else null end as 'Type.Score.IsDisplayable',
                case when FT.Type = 'Score' then FT.IsListable else null end as 'Type.Score.IsListable',
                case when FT.Type = 'Score' then FT.IsPrimaryFilter else null end as 'Type.Score.IsPrimaryFilter',
                case when FT.Type = 'Score' then FT.ShowIfEmpty else null end as 'Type.Score.ShowIfEmpty',
                case when FT.Type = 'Score' then FT.DisplayInColumn else null end as 'Type.Score.DisplayInColumn', 


		        case when FT.Type = 'Counter' then FT.ColumnOrder else null end as 'Type.Counter.ColumnOrder',
		        case when FT.Type = 'Counter' then FT.ColumnWidth else null end as 'Type.Counter.ColumnWidth',
		        case when FT.Type = 'Counter' then FT.SortOrder else null end as 'Type.Counter.SortOrder',
		        case when FT.Type = 'Counter' then TRY_CAST(FT.DefaultValue as datetime) else null end as 'Type.Counter.DefaultValue',
		        case when FT.Type = 'Counter' then FT.DisplayDescription else null end as 'Type.Counter.Description.Display',
		        case when FT.Type = 'Counter' then FT.FormDescription else null end as 'Type.Counter.Description.Form',
		        case when FT.Type = 'Counter' then FT.IsRequired else null end as 'Type.Counter.Validation.IsRequired',
		        case when FT.Type = 'Counter' then FT.IsDisplayable else null end as 'Type.Counter.IsDisplayable',
		        case when FT.Type = 'Counter' then FT.IsEditable else null end as 'Type.Counter.IsEditable',
		        case when FT.Type = 'Counter' then FT.IsListable else null end as 'Type.Counter.IsListable',
		        case when FT.Type = 'Counter' then FT.IsPartOfKey else null end as 'Type.Counter.IsPartOfKey',
		        case when FT.Type = 'Counter' then FT.IsPrimaryFilter else null end as 'Type.Counter.IsPrimaryFilter',
		        case when FT.Type = 'Counter' then FT.ShowIfEmpty else null end as 'Type.Counter.ShowIfEmpty',
                case when FT.Type = 'Counter' then FT.SearchAddToResult else null end as 'Type.Counter.Search.AddToResult', 
                case when FT.Type = 'Counter' then FT.SearchPrefix else null end as 'Type.Counter.Search.Prefix', 
                case when FT.Type = 'Counter' then FT.SearchSuffix else null end as 'Type.Counter.Search.Suffix', 
                case when FT.Type = 'Counter' then FT.SearchDisplayOrder else null end as 'Type.Counter.Search.DisplayOrder',
                case when FT.Type = 'Counter' then FT.CounterPrefix else null end as 'Type.Counter.CounterPrefix', 
                case when FT.Type = 'Counter' then FT.CounterInitialIndex else null end as 'Type.Counter.CounterInitialIndex',
                case when FT.Type = 'Counter' then FT.DisplayInColumn else null end as 'Type.Counter.DisplayInColumn'

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
        {orderByClause}
        offset ((@pageNum-1) * @pageSize) rows fetch next @pageSize rows only
        for json path
        ) as 'items'
for json path, WITHOUT_ARRAY_WRAPPER";

            var model = await Company.GetDatabaseJsonAsObjectAsync<FieldTypesApiViewModel>(sql, dbArgs, ApiTimeout);
            return new Tuple<FieldTypesApiViewModel, WorkHttpStatus>(model, workHttpStatus);
        }

        internal class FieldInfo
        {
            public int FieldTypeID { get; set; }
            public AssetTypeClass Class { get; set; }
            public string FieldType { get; set; }
        }
        public WorkHttpStatus UpdateFields(FieldTypesApiEditModel model, TypeIdentifierInfoModel typeIdentifierInfoModel)
        {
            var currentFieldTypes = Company.Filter<FieldType>(f => f.Object == typeIdentifierInfoModel.Object && f.ObjectID == typeIdentifierInfoModel.ObjectID, i => i.FieldTypeLookup).ToList();
            var existingKeyFields = string.Join("|", currentFieldTypes.Where(f => f.IsPartOfKey).Select(f => f.ID).OrderBy(f => f));

            var newFieldTypes = new List<FieldType>();

            var fieldTypeNamesToDelete = new List<string>();
            var allowedConversions = DataType.Boolean.GetAllowedConversionOptions();
            var reservedWords = new List<string>() { "color", "icon", "parentid", "database", "path", "keypath", "displaypath" };
            var maxColumnIndexItem = currentFieldTypes.OrderByDescending(x => x.ColumnOrder).FirstOrDefault();
            var maxColumnIndex = 0;
            if (maxColumnIndexItem != null)
                maxColumnIndex = maxColumnIndexItem.ColumnOrder;

            foreach (var f in model.Fields)
            {
                if (reservedWords.Contains(f.Name.ToLower()))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You may not use {f.Name} as the Name of your field because it is a reserved word.");
                }

                var newFieldType = new FieldType
                {
                    AssetTypeID = typeIdentifierInfoModel.ID,
                    Object = typeIdentifierInfoModel.Object,
                    ObjectID = typeIdentifierInfoModel.ObjectID,
                    Category = f.Category,
                    Name = f.Name,
                    FriendlyName = f.FriendlyName,
                    UpdatedBy = Company.CurrentResourceID
                };

                if (f.Type.Boolean != null)
                {
                    newFieldType.Type = DataType.Boolean.ToString();
                    newFieldType.ColumnOrder = f.Type.Boolean.ColumnOrder.HasValue ? f.Type.Boolean.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Boolean.ColumnWidth;
                    if (f.Type.Boolean.DefaultValue.HasValue)
                    {
                        newFieldType.DefaultValue = f.Type.Boolean.DefaultValue.Value.ToString().ToLower();
                        newFieldType.DefaultFormattedValue = newFieldType.DefaultValue;
                    }
                    if (f.Type.Boolean.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Boolean.Description.Display;
                        newFieldType.FormDescription = f.Type.Boolean.Description.Form;
                    }
                    if (f.Type.Boolean.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Boolean.Validation.IsRequired;
                    }

                    newFieldType.IsDisplayable = f.Type.Boolean.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Boolean.IsEditable;
                    newFieldType.IsListable = f.Type.Boolean.IsListable;
                    newFieldType.IsPartOfKey = f.Type.Boolean.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.Boolean.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Boolean.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Boolean.SortOrder;
                    newFieldType.DisplayInColumn = f.Type.Boolean.DisplayInColumn;

                    if (f.Type.Boolean.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.Boolean.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.Boolean.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.Boolean.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.Boolean.Search.DisplayOrder;
                    }
                }
                else if (f.Type.Score != null)
                {
                    if (model.ActionTypeUid.HasValue || model.RelationshipTypeUid.HasValue)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You may not use a Score type on an action type or relationship type for field {f.Name}.");
                    }

                    var assetType = Company.Filter<AssetType>(a => a.uid == model.AssetTypeUid).FirstOrDefault();

                    var disallowedClasses = new List<AssetTypeClass>() {
                        AssetTypeClass.Organization,
                        AssetTypeClass.User,
                        AssetTypeClass.ReferenceItemType
                    };

                    if (disallowedClasses.Contains(assetType.Class))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You may not use a Score type on an asset of type {assetType.Class.ToString()} for field {f.Name}.");
                    }

                    var types = Company.Query<int>(
                   "select distinct ScoreType from metrics.Allocation where AssetTypeUid = @uid and [State] = 1"
                   , new { assetType.uid }).ToList();

                    if (!types.Contains((int)f.Type.Score.ScoreType))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"Score type {f.Type.Score.ScoreType.ToString()} cannot be allocated to this asset type for field {f.Name}.");
                    }

                    newFieldType.Type = DataType.Score.ToString();
                    newFieldType.ScoreType = (int)f.Type.Score.ScoreType;
                    newFieldType.IsDisplayable = f.Type.Score.IsDisplayable;
                    newFieldType.IsEditable = false;
                    newFieldType.IsListable = f.Type.Score.IsListable;
                    newFieldType.IsPartOfKey = false;
                    newFieldType.IsPrimaryFilter = f.Type.Score.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Score.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Score.SortOrder;
                    newFieldType.ColumnWidth = f.Type.Score.ColumnWidth;
                    newFieldType.ColumnOrder = f.Type.Score.ColumnOrder.HasValue ? f.Type.Score.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Score.ColumnWidth;
                    newFieldType.DisplayInColumn = f.Type.Score.DisplayInColumn;

                    if (f.Type.Score.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Score.Description.Display;
                    }

                }
                else if (f.Type.ComputedOwnershipLookup != null)
                {
                    if (model.ActionTypeUid.HasValue || model.RelationshipTypeUid.HasValue)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You may not use a Ownership Lookup type on an action type or relationship type for field {f.Name}.");
                    }

                    newFieldType.Type = DataType.OwnershipLookup.ToString();
                    newFieldType.ColumnOrder = f.Type.ComputedOwnershipLookup.ColumnOrder.HasValue ? f.Type.ComputedOwnershipLookup.ColumnOrder.Value : ++maxColumnIndex;
                    if (f.Type.ComputedOwnershipLookup.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.ComputedOwnershipLookup.Description.Display;
                    }
                    newFieldType.IsDisplayable = f.Type.ComputedOwnershipLookup.IsDisplayable;
                    newFieldType.IsEditable = false;
                    newFieldType.IsListable = f.Type.ComputedOwnershipLookup.IsListable;
                    newFieldType.IsPartOfKey = false;
                    newFieldType.IsPrimaryFilter = false;
                    newFieldType.ShowIfEmpty = f.Type.ComputedOwnershipLookup.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.ComputedOwnershipLookup.SortOrder;
                    newFieldType.ColumnWidth = f.Type.ComputedOwnershipLookup.ColumnWidth;

                    newFieldType.DisplayInColumn = f.Type.ComputedOwnershipLookup.DisplayInColumn;

                    if (f.Type.ComputedOwnershipLookup.Definition.ResponsibilityTypeUid != null)
                    {
                        int relationshipsTypeId = Company.Query<int>(@"SELECT id FROM [dbo].[ResponsibilityType] WHERE uid = @uid", new
                        {
                            uid = f.Type.ComputedOwnershipLookup.Definition.ResponsibilityTypeUid
                        }).FirstOrDefault();
                        f.Type.ComputedOwnershipLookup.Definition.ResponsibilityType = relationshipsTypeId;
                        f.Type.ComputedOwnershipLookup.Definition.ResponsibilityTypeUid = null;
                    }

                    newFieldType.FieldTypeLookup = new FieldTypeLookup
                    {
                        HideFilter = f.Type.ComputedOwnershipLookup.HideFilter,
                        HideFooter = f.Type.ComputedOwnershipLookup.HideFooter,
                        HideHeader = f.Type.ComputedOwnershipLookup.HideHeader,
                        LookupType = 0,
                        Definition = JsonConvert.SerializeObject(f.Type.ComputedOwnershipLookup.Definition, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                    };
                }
                else if (f.Type.ComputedRelationshipField != null)
                {
                    newFieldType.Type = DataType.FieldFromRelationship.ToString();
                    newFieldType.ColumnOrder = f.Type.ComputedRelationshipField.ColumnOrder.HasValue ? f.Type.ComputedRelationshipField.ColumnOrder.Value : ++maxColumnIndex;
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
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Relationship Type/Field not found", $"Relationship Type or Field Type not found based on Uid provided [{f.Type.ComputedRelationshipField.IntersectTypeUid}].");
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
                    newFieldType.DisplayInColumn = f.Type.ComputedRelationshipField.DisplayInColumn;

                    if (f.Type.ComputedRelationshipField.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.ComputedRelationshipField.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.ComputedRelationshipField.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.ComputedRelationshipField.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.ComputedRelationshipField.Search.DisplayOrder;
                    }
                }
                else if (f.Type.ComputedRelationshipLookup != null)
                {

                    if (model.ActionTypeUid.HasValue || model.RelationshipTypeUid.HasValue)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You may not use a Relationship Lookup type on an action type or relationship type for field {f.Name}.");
                    }

                    var assetType = Company.Filter<AssetType>(a => a.uid == model.AssetTypeUid).FirstOrDefault();

                    if (assetType.Class == AssetTypeClass.User)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You may not use a ComputedRelationshipLookup type on an asset of type {assetType.Class.ToString()} for field {f.Name}.");
                    }

                    if (f.Type.ComputedRelationshipLookup.Definition == null
                        || !f.Type.ComputedRelationshipLookup.Definition.Fields.Any()
                        || !f.Type.ComputedRelationshipLookup.Definition.Relations.Any())
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You must provide a definition for the computed relationship lookup field {f.Name}.");
                    }

                    newFieldType.Type = DataType.ComplexRelationLookup.ToString();
                    newFieldType.ColumnOrder = f.Type.ComputedRelationshipLookup.ColumnOrder.HasValue ? f.Type.ComputedRelationshipLookup.ColumnOrder.Value : ++maxColumnIndex;
                    if (f.Type.ComputedRelationshipLookup.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.ComputedRelationshipLookup.Description.Display;
                    }
                    newFieldType.IsDisplayable = f.Type.ComputedRelationshipLookup.IsDisplayable;
                    newFieldType.ShowIfEmpty = f.Type.ComputedRelationshipLookup.ShowIfEmpty;

                    #region build definition

                    var definitionFields = new List<FieldTypeComplexLookupDefinitionField>();
                    var definitionRelations = new List<FieldTypeComplexLookupDefinitionRelation>();
                    var hasDefinitionError = false;
                    var definitionErrorMessage = $"The definition provided for the computed relationship lookup {f.Name} has one or more invalid uids.";
                    var computedFields = new Dictionary<string, int>() { { "DisplayValue", 0 }, { "_assetPath", 0 } };
                    var relatedItemUids = new List<Guid>();

                    f.Type.ComputedRelationshipLookup.Definition.Relations.ForEach(i =>
                    {
                        var relation = new FieldTypeComplexLookupDefinitionRelation();
                        var relationInfo = Company.Query<dynamic>(
                            "select T.ID as IntersectTypeID, A.Object, A.ObjectID from IntersectType T left join AssetType A on A.uid = @uid where T.uid = @intersectUid",
                            new { uid = i.AssetTypeUid, intersectUid = i.IntersectTypeUid }
                        ).SingleOrDefault();

                        if (relationInfo == null || i.RelationType == null)
                        {
                            hasDefinitionError = true;
                            return;
                        }

                        var RelationRefListInfo = Company.Query<dynamic>(
                            "select T.ID as IntersectTypeID from IntersectType T where T.uid = @intersectUid and ((T.object = @AssetType and T.ObjectID = 0) or (T.Subject = @AssetType and T.SubjectID = 0))",
                            new { uid = i.AssetTypeUid, intersectUid = i.IntersectTypeUid, AssetType = SystemObjects.ReferenceItemType.ToString() }
                        ).SingleOrDefault();

                        if (RelationRefListInfo != null)
                        {
                            hasDefinitionError = true;
                            return;
                        }

                        string relationObject = relationInfo.Object;
                        int relationObjectId = relationInfo.ObjectID;
                        int relationIntersectId = relationInfo.IntersectTypeID;

                        relation.Direction = i.Direction ?? FieldTypeComplexLookupRelationDirection.Forward;
                        relation.RelationType = i.RelationType ?? ComplexLookupRelationType.StandardRelationship;
                        relation.AssetTypeUid = i.AssetTypeUid;
                        relation.IntersectTypeUid = i.IntersectTypeUid;

                        relatedItemUids.Add(i.AssetTypeUid);
                        definitionRelations.Add(relation);

                        var relatedTypeList = Company.Filter<IntersectTypeDetail>(r =>
                           (r.Subject == relationObject && r.SubjectID == relationObjectId) ||
                           (r.Object == relationObject && r.ObjectID == relationObjectId)
                           )
                        .ToList()
                        .Select(r => new
                        {
                            r.ID,
                            Name = (r.Subject == relationObject && r.SubjectID == relationObjectId)
                               ? $"{r.ObjectName} ({r.PredicateName})"
                               : $"{r.SubjectName} ({r.PredicateName})"
                        })
                        .Distinct()
                        .ToList();

                        relatedTypeList.ForEach(r =>
                        {
                            var fieldName = $"Related Item.{r.Name} ({r.ID})";

                            if (!computedFields.ContainsKey(fieldName))
                            {
                                computedFields.Add(fieldName, r.ID);
                            }
                        });

                    });

                    f.Type.ComputedRelationshipLookup.Definition.Fields.ForEach(i =>
                    {

                        bool bypassFieldValidation = false;
                        var field = new FieldTypeComplexLookupDefinitionField();
                        var isRelatedItem = i.FieldTypeName.StartsWith("Related Item.");
                        var isFieldFromRelationship = i.FieldTypeName.StartsWith("Relation.");

                        var fieldInfo = Company.Query<FieldInfo>(@"
                            select coalesce(F.ID, 0) as FieldTypeID, T.Class, F.Type  as FieldType
                            from   AssetType T 
                                   left join FieldType F on F.AssetTypeID = T.ID and F.Name = @FieldTypeName 
                            where  T.uid = @AssetTypeUid",
                            new { i.FieldTypeName, i.AssetTypeUid }).SingleOrDefault();

                        if (isFieldFromRelationship)
                        {
                            var relation = f.Type.ComputedRelationshipLookup.Definition.Relations.FirstOrDefault(x => x.AssetTypeUid == i.AssetTypeUid);
                            var intersectTypeUid = relation.IntersectTypeUid;
                            var fieldName = i.FieldTypeName.Replace("Relation.", "").Trim();
                            fieldInfo = Company.Query<FieldInfo>(@"
                            select coalesce(F.ID, 0) as FieldTypeID, 0 as Class, F.Type as FieldType
                            from   IntersectType IT 
                                   left join FieldType F on F.Object = 'IntersectType' and F.ObjectID = IT.Id and F.Name = @fieldName 
                            where  IT.uid = @intersectTypeUid",
                            new { fieldName, intersectTypeUid }).SingleOrDefault();
                        }

                        // Invalid uid
                        if ((isRelatedItem && !relatedItemUids.Contains(i.AssetTypeUid)) || fieldInfo == null)
                        {
                            hasDefinitionError = true;
                            return;
                        }

                        //field from relationship types are not supported on relationship lookups
                        if (DataType.Text.GetNotAllowedInRelationshipLookup().Contains(fieldInfo.FieldType))
                        {
                            hasDefinitionError = true;
                            definitionErrorMessage = $@"The definition provided for the computed relationship lookup {f.Name} is invalid. Field from relationships are not supported on compuited relation lookup fields.";
                            return;
                        }

                        // Skip this validation for hard-coded fields on certain types.
                        if (fieldInfo.Class == AssetTypeClass.Reference && i.AssetTypeUid == Guid.Empty && new[] { "Name", "Description" }.Contains(i.FieldTypeName))
                            bypassFieldValidation = true;
                        else if (fieldInfo.Class == AssetTypeClass.Reference && i.AssetTypeUid != Guid.Empty && new[] { "Code" }.Contains(i.FieldTypeName))
                            bypassFieldValidation = true;
                        else if (fieldInfo.Class == AssetTypeClass.User && new[] { "FirstName", "LastName", "Email", "LastLoggedInOn", "DisplayValue" }.Contains(i.FieldTypeName))
                            bypassFieldValidation = true;

                        // Invalid computed field
                        if (fieldInfo.FieldTypeID == 0 && !bypassFieldValidation)
                        {
                            if (!computedFields.ContainsKey(i.FieldTypeName))
                            {
                                hasDefinitionError = true;
                                return;
                            }
                        }
                        var computedFieldValue = computedFields.ContainsKey(i.FieldTypeName) ? computedFields[i.FieldTypeName] : 0;
                        field.FieldTypeID = (fieldInfo.FieldTypeID == 0) ? computedFieldValue : fieldInfo.FieldTypeID;
                        field.AssetTypeUid = i.AssetTypeUid;
                        field.DisplayOrder = i.DisplayOrder;
                        field.FieldTypeName = i.FieldTypeName;
                        field.Filter = i.Filter;
                        if (string.IsNullOrEmpty(i.OverrideDisplayName) || string.IsNullOrWhiteSpace(i.OverrideDisplayName))
                        {
                            i.OverrideDisplayName = null;
                        }
                        field.OverrideDisplayName = i.OverrideDisplayName;
                        field.SortOrder = i.SortOrder;
                        field.Width = i.Width;
                        field.Show = i.Show;
                        if (i.RelationIndex != null)
                        {
                            if (definitionRelations[i.RelationIndex ?? 0].AssetTypeUid != field.AssetTypeUid)
                            {
                                hasDefinitionError = true;
                                definitionErrorMessage = $@"The definition provided for the computed relationship lookup {f.Name} is invalid. Field {i.FieldTypeName} does not match Asset Type.";
                                return;
                            }
                            field.RelationIndex = i.RelationIndex;
                        }
                        else
                        {
                            field.RelationIndex = definitionRelations.FindIndex(r => r.AssetTypeUid == field.AssetTypeUid);
                        }

                        if (!definitionFields.Any(o => o.FieldTypeID == field.FieldTypeID && o.RelationIndex == field.RelationIndex) && field.FieldTypeID > 0)
                        {
                            definitionFields.Add(field);
                        }
                        else if (!definitionFields.Any(o => o.FieldTypeName == field.FieldTypeName && o.AssetTypeUid == field.AssetTypeUid) && field.FieldTypeID == 0)
                        {
                            definitionFields.Add(field);
                        }
                    });



                    if (hasDefinitionError)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", definitionErrorMessage);
                    }

                    #endregion

                    newFieldType.FieldTypeLookup = new FieldTypeLookup
                    {
                        HideFilter = f.Type.ComputedRelationshipLookup.HideFilter,
                        HideFooter = f.Type.ComputedRelationshipLookup.HideFooter,
                        HideHeader = f.Type.ComputedRelationshipLookup.HideHeader,
                        LookupType = 0,
                        Definition = JsonConvert.SerializeObject(new
                        {
                            Relations = definitionRelations,
                            Fields = definitionFields
                        })
                    };
                }
                else if (f.Type.ComputedRelationshipReferenceList != null)
                {
                    if (model.ActionTypeUid.HasValue || model.RelationshipTypeUid.HasValue)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You may not use a Reference Item List from Relationship type on an action type or relationship type for field {f.Name}.");
                    }

                    newFieldType.Type = DataType.RefListRelationship.ToString();
                    newFieldType.ColumnOrder = f.Type.ComputedRelationshipReferenceList.ColumnOrder.HasValue ? f.Type.ComputedRelationshipReferenceList.ColumnOrder.Value : ++maxColumnIndex;
                    if (f.Type.ComputedRelationshipReferenceList.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.ComputedRelationshipReferenceList.Description.Display;
                    }
                    newFieldType.IsDisplayable = f.Type.ComputedRelationshipReferenceList.IsDisplayable;
                    newFieldType.ShowIfEmpty = f.Type.ComputedRelationshipReferenceList.ShowIfEmpty;
                    var relationshipsFieldType = Company.Query<int>(@"select ID from IntersectType where Uid = @uid", new { uid = f.Type.ComputedRelationshipReferenceList.IntersectTypeUid }).FirstOrDefault();
                    if (relationshipsFieldType <= 0)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Relationship Type not found", $"Relationship Type or Field Type not found based on Uid provided [{f.Type.ComputedRelationshipReferenceList.IntersectTypeUid}].");
                    }
                    newFieldType.LookupObjectType = "IntersectType";
                    newFieldType.LookupObjectID = relationshipsFieldType;
                    newFieldType.Definition = JsonConvert.SerializeObject(new { f.Type.ComputedRelationshipReferenceList.DisplayRefListDescription });
                }
                else if (f.Type.Date != null)
                {
                    newFieldType.Type = DataType.Date.ToString();
                    newFieldType.ColumnOrder = f.Type.Date.ColumnOrder.HasValue ? f.Type.Date.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Date.ColumnWidth;
                    if (f.Type.Date.DefaultValue.HasValue)
                    {
                        newFieldType.DefaultValue = f.Type.Date.DefaultValue.Value.ToString("M/d/yyyy");
                        newFieldType.DefaultFormattedValue = newFieldType.DefaultValue;

                    }
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
                    if (f.Type.Date.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.Date.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.Date.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.Date.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.Date.Search.DisplayOrder;
                    }
                    newFieldType.DisplayInColumn = f.Type.Date.DisplayInColumn;

                }
                else if (f.Type.DateTime != null)
                {
                    newFieldType.Type = DataType.DateTime.ToString();
                    newFieldType.ColumnOrder = f.Type.DateTime.ColumnOrder.HasValue ? f.Type.DateTime.ColumnOrder.Value : ++maxColumnIndex;
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
                    newFieldType.DisplayInColumn = f.Type.DateTime.DisplayInColumn;

                    if (f.Type.DateTime.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.DateTime.Validation.IsRequired;
                    }
                    if (f.Type.DateTime.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.DateTime.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.DateTime.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.DateTime.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.DateTime.Search.DisplayOrder;
                    }
                }
                else if (f.Type.Decimal != null)
                {
                    newFieldType.Type = DataType.Decimal.ToString();
                    newFieldType.ColumnOrder = f.Type.Decimal.ColumnOrder.HasValue ? f.Type.Decimal.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Decimal.ColumnWidth;
                    if (f.Type.Decimal.DefaultValue.HasValue)
                    {
                        newFieldType.DefaultValue = f.Type.Decimal.DefaultValue.Value.ToString();
                        newFieldType.DefaultFormattedValue = newFieldType.DefaultValue;
                    }
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
                    newFieldType.DisplayInColumn = f.Type.Decimal.DisplayInColumn;


                    if (f.Type.Decimal.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Decimal.Validation.IsRequired;
                        newFieldType.MaximumLength = f.Type.Decimal.Validation.MaximumValue;
                        newFieldType.MinimumLength = f.Type.Decimal.Validation.MinimumValue;
                        newFieldType.Precision = f.Type.Decimal.Validation.Precision;
                    }
                    if (f.Type.Decimal.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.Decimal.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.Decimal.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.Decimal.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.Decimal.Search.DisplayOrder;
                    }
                }
                else if (f.Type.Html != null)
                {
                    newFieldType.Type = DataType.Html.ToString();
                    newFieldType.ColumnOrder = f.Type.Html.ColumnOrder.HasValue ? f.Type.Html.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Html.ColumnWidth;

                    newFieldType.DefaultValue = f.Type.Html.DefaultValue;
                    newFieldType.DefaultFormattedValue = newFieldType.DefaultValue;

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
                    newFieldType.DisplayInColumn = f.Type.Html.DisplayInColumn;

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
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You may not use a JSON type on an action type or relationship type for field {f.Name}.");
                    }
                    newFieldType.Type = DataType.JSON.ToString();
                    newFieldType.ColumnOrder = f.Type.Json.ColumnOrder.HasValue ? f.Type.Json.ColumnOrder.Value : ++maxColumnIndex;
                    if (f.Type.Json.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Json.Description.Display;
                    }
                    newFieldType.IsDisplayable = f.Type.Json.IsDisplayable;
                    newFieldType.ShowIfEmpty = f.Type.Json.ShowIfEmpty;
                    if (f.Type.Json.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Json.Validation.IsRequired;
                    }
                }
                else if (f.Type.JsonElement != null)
                {
                    if (model.ActionTypeUid.HasValue || model.RelationshipTypeUid.HasValue)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You may not use a JSON Element type on an action type or relationship type for field {f.Name}.");
                    }
                    newFieldType.Type = DataType.JsonElement.ToString();
                    newFieldType.ColumnOrder = f.Type.JsonElement.ColumnOrder.HasValue ? f.Type.JsonElement.ColumnOrder.Value : ++maxColumnIndex;
                    if (f.Type.JsonElement.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.JsonElement.Description.Display;
                    }
                    newFieldType.IsDisplayable = f.Type.JsonElement.IsDisplayable;
                    newFieldType.ShowIfEmpty = f.Type.JsonElement.ShowIfEmpty;
                    newFieldType.IsListable = f.Type.JsonElement.IsListable;
                    if (f.Type.JsonElement.JsonAttribute != null)
                    {
                        int FieldTypeID = Company.FieldTypes.FirstOrDefault(ft => ft.Object == newFieldType.Object && ft.ObjectID == newFieldType.ObjectID && ft.Name == f.Type.JsonElement.JsonAttribute.FieldName).ID;
                        var obj = new { FieldTypeID, f.Type.JsonElement.JsonAttribute.Path, f.Type.JsonElement.JsonAttribute.DataType };
                        newFieldType.Definition = JsonConvert.SerializeObject(obj);
                    }
                }
                else if (f.Type.Link != null)
                {
                    newFieldType.Type = DataType.Link.ToString();
                    newFieldType.ColumnOrder = f.Type.Link.ColumnOrder.HasValue ? f.Type.Link.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Link.ColumnWidth;
                    if (f.Type.Link.DefaultValue != null)
                    {
                        if (string.IsNullOrEmpty(f.Type.Link.DefaultValue.Text) || string.IsNullOrWhiteSpace(f.Type.Link.DefaultValue.Text))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You must provide a link Text value if setting a default value for {f.Name}.");
                        }
                        if (string.IsNullOrEmpty(f.Type.Link.DefaultValue.Url) || string.IsNullOrWhiteSpace(f.Type.Link.DefaultValue.Url))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"You must provide a link Url value if setting a default value for {f.Name}.");
                        }
                        newFieldType.DefaultValue = $"{f.Type.Link.DefaultValue.Text}|{f.Type.Link.DefaultValue.Url}";
                    }
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
                    newFieldType.DisplayInColumn = f.Type.Link.DisplayInColumn;

                    if (f.Type.Link.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Link.Validation.IsRequired;
                    }
                    if (f.Type.Link.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.Link.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.Link.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.Link.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.Link.Search.DisplayOrder;
                    }
                }
                else if (f.Type.Lookup != null)
                {
                    newFieldType.Type = DataType.Lookup.ToString();
                    newFieldType.ColumnOrder = f.Type.Lookup.ColumnOrder.HasValue ? f.Type.Lookup.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Lookup.ColumnWidth;
                    if (!string.IsNullOrEmpty(f.Type.Lookup.ParentFieldTypeName))
                    {
                        var parentField = Company.Filter<FieldType>(x => x.AssetTypeID == typeIdentifierInfoModel.ID && x.Name == f.Type.Lookup.ParentFieldTypeName).SingleOrDefault();
                        if (parentField == null || parentField.LookupObjectType != "ReferenceItem")
                        {
                            return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid parent Field", $"Parent field [{f.Type.Lookup.ParentFieldTypeName}] of type ReferenceItem not found on this asset.");
                        }
                        newFieldType.ParentFieldTypeID = parentField.ID;
                    }
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
                            var defaultOptions = Company.Filter<Asset>(a => a.AssetTypeID == listAssetType.ID);
                            if (listAssetType != null)
                            {
                                newFieldType.LookupObjectType = listAssetType.Object.Replace("Type", "");
                                newFieldType.LookupObjectID = listAssetType.ObjectID;
                                if (!string.IsNullOrEmpty(f.Type.Lookup.DefaultValue) && defaultOptions.Any(s => s.uid.ToString() == f.Type.Lookup.DefaultValue))
                                {
                                    int defaultListItemID = defaultOptions.First(s => s.uid.ToString() == f.Type.Lookup.DefaultValue).ObjectID;
                                    newFieldType.DefaultValue = defaultListItemID.ToString();
                                }
                            }
                            else
                            {
                                return new WorkHttpStatus(HttpStatusCode.NotFound, "List Asset Type not found", $"Asset Type not found for field [{f.Name}].");
                            }
                        }
                        else if (f.Type.Lookup.List.Class.HasValue && !f.Type.Lookup.List.Uid.HasValue)
                        {
                            if (f.Type.Lookup.List.Class.Value == AssetTypeClass.Model)
                            {
                                newFieldType.LookupObjectType = "TaxonomyType";
                                newFieldType.LookupObjectID = 0;
                            }
                            else if (f.Type.Lookup.List.Class.Value == AssetTypeClass.ReferenceItemType)
                            {
                                newFieldType.LookupObjectType = "ReferenceItemType";
                                newFieldType.LookupObjectID = 0;
                            }
                            else
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field Type - list not specified", $"Lookup Field Type is incomplete as it does not have a valid class specified.");
                            }
                        }
                        else if (!f.Type.Lookup.List.Class.HasValue && f.Type.Lookup.List.Uid.HasValue)
                        {
                            var listAssetType = Company.Filter<AssetType>(i => i.uid == f.Type.Lookup.List.Uid.Value).SingleOrDefault();
                            var defaultOptions = Company.Filter<Asset>(a => a.AssetTypeID == listAssetType.ID);
                            if (listAssetType != null)
                            {
                                newFieldType.LookupObjectType = listAssetType.Object.Replace("Type", "");
                                newFieldType.LookupObjectID = listAssetType.ObjectID;
                                if (!string.IsNullOrEmpty(f.Type.Lookup.DefaultValue) && defaultOptions.Any(s => s.uid.ToString() == f.Type.Lookup.DefaultValue))
                                {
                                    int defaultListItemID = defaultOptions.First(s => s.uid.ToString() == f.Type.Lookup.DefaultValue).ObjectID;
                                    newFieldType.DefaultValue = defaultListItemID.ToString();
                                }
                            }
                            else
                            {
                                return new WorkHttpStatus(HttpStatusCode.NotFound, "List Asset Type not found", $"Asset Type not found for field [{f.Name}].");
                            }
                        }
                        else
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field Type - list not specified", $"Lookup Field Type is incomplete as it does not have a List specified.");
                        }
                    }
                    else
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field Type - list not specified", $"Lookup Field Type is incomplete as it does not have a List specified.");
                    }
                    if (f.Type.Lookup.Filter != null)
                    {
                        int? filterFieldType = null;
                        int? filterPredicate = null;
                        bool? filterPredicateDirection = null;
                        if (!string.IsNullOrEmpty(f.Type.Lookup.Filter.FieldTypeName))
                        {
                            filterFieldType = Company.Query<int>(@"select ID from FieldType where Object = @t and ObjectID = @tid and Name = @n", new { t = typeIdentifierInfoModel.Object, tid = typeIdentifierInfoModel.ObjectID, n = f.Type.Lookup.Filter.FieldTypeName }).FirstOrDefault();
                            if (filterFieldType <= 0)
                            {
                                return new WorkHttpStatus(HttpStatusCode.NotFound, "Field Type not found", $"Field Type not found based on Name provided [{f.Type.Lookup.Filter.FieldTypeName}].");
                            }
                        }
                        else if (string.IsNullOrEmpty(f.Type.Lookup.Filter.FieldTypeName) && typeIdentifierInfoModel.Object == SystemObjects.IssueType.ToString())
                        {
                            //IssueTypes can have a Filter just based on Preidcate/Predicate direction. That will be Action/Subject and the filterFieldType is null
                            filterFieldType = null;
                        }
                        if (f.Type.Lookup.Filter.PredicateUid.HasValue && f.Type.Lookup.Filter.PredicateUid != Guid.Empty)
                        {
                            filterPredicate = Company.Query<int>(@"select ID from [Predicate] where Uid = @uid", new { uid = f.Type.Lookup.Filter.PredicateUid }).FirstOrDefault();
                            if (filterPredicate <= 0)
                            {
                                return new WorkHttpStatus(HttpStatusCode.NotFound, "Field Type not found", $"Field Type not found based on Name provided [{f.Type.Lookup.Filter.FieldTypeName}].");
                            }
                            filterPredicateDirection = f.Type.Lookup.Filter.UseDirection;
                        }
                        newFieldType.FilterFieldTypeID = filterFieldType;
                        newFieldType.FilterPredicateID = filterPredicate;
                        newFieldType.FilterPredicateDirection = filterPredicateDirection;
                    }
                    if (f.Type.Lookup.Format != null && !string.IsNullOrEmpty(f.Type.Lookup.Format.Display))
                    {
                        newFieldType.LookupDisplayFormat = f.Type.Lookup.Format.Display;
                        newFieldType.LookupEditFormat = string.IsNullOrEmpty(f.Type.Lookup.Format.Edit) ? f.Type.Lookup.Format.Display : f.Type.Lookup.Format.Edit;
                    }
                    else
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid Lookup display Format", $"List Display Format is required for Lookup Field Type.");
                    }
                    newFieldType.IsDisplayable = f.Type.Lookup.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Lookup.IsEditable;
                    newFieldType.IsListable = f.Type.Lookup.IsListable;
                    newFieldType.IsPartOfKey = f.Type.Lookup.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.Lookup.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Lookup.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Lookup.SortOrder;
                    newFieldType.DisplayInColumn = f.Type.Lookup.DisplayInColumn;

                    if (f.Type.Lookup.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Lookup.Validation.IsRequired;
                    }
                    if (f.Type.Lookup.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.Lookup.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.Lookup.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.Lookup.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.Lookup.Search.DisplayOrder;
                    }
                }
                else if (f.Type.Number != null)
                {
                    newFieldType.Type = DataType.Number.ToString();
                    newFieldType.ColumnOrder = f.Type.Number.ColumnOrder.HasValue ? f.Type.Number.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Number.ColumnWidth;
                    if (f.Type.Number.DefaultValue.HasValue)
                    {
                        newFieldType.DefaultValue = f.Type.Number.DefaultValue.Value.ToString();
                        newFieldType.DefaultFormattedValue = newFieldType.DefaultValue;
                    }
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
                    newFieldType.DisplayInColumn = f.Type.Number.DisplayInColumn;

                    if (f.Type.Number.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Number.Validation.IsRequired;
                        newFieldType.MaximumLength = f.Type.Number.Validation.MaximumValue;
                        newFieldType.MinimumLength = f.Type.Number.Validation.MinimumValue;
                    }
                    if (f.Type.Number.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.Number.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.Number.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.Number.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.Number.Search.DisplayOrder;
                    }
                }
                else if (f.Type.Path != null)
                {
                    newFieldType.Type = DataType.Path.ToString();
                    newFieldType.ColumnOrder = f.Type.Path.ColumnOrder.HasValue ? f.Type.Path.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Path.ColumnWidth;
                    if (f.Type.Path.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Path.Description.Display;
                    }
                    newFieldType.IsDisplayable = f.Type.Path.IsDisplayable;
                    newFieldType.IsEditable = false;
                    newFieldType.IsListable = f.Type.Path.IsListable;
                    newFieldType.IsPartOfKey = false;
                    newFieldType.ShowIfEmpty = true;
                    newFieldType.SortOrder = f.Type.Path.SortOrder;
                    newFieldType.IsPrimaryFilter = false;
                    newFieldType.DisplayInColumn = f.Type.Path.DisplayInColumn;

                }
                else if (f.Type.Relationship != null)
                {
                    newFieldType.Type = DataType.Relationship.ToString();
                    newFieldType.ColumnOrder = f.Type.Relationship.ColumnOrder.HasValue ? f.Type.Relationship.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Relationship.ColumnWidth;
                    if (f.Type.Relationship.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Relationship.Description.Display;
                        newFieldType.FormDescription = f.Type.Relationship.Description.Form;
                    }
                    var relationshipType = Company.Query<int>(@"select ID from IntersectType where Uid = @uid", new { uid = f.Type.Relationship.IntersectTypeUid }).FirstOrDefault();
                    if (relationshipType <= 0)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Relationship Type not found", $"Relationship Type not found based on Uid provided [{f.Type.Relationship.IntersectTypeUid}].");
                    }
                    newFieldType.LookupObjectType = "IntersectType";
                    newFieldType.LookupObjectID = relationshipType;
                    newFieldType.IsDisplayable = f.Type.Relationship.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Relationship.IsEditable;
                    newFieldType.IsListable = f.Type.Relationship.IsListable;
                    newFieldType.ShowIfEmpty = f.Type.Relationship.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Relationship.SortOrder;
                    newFieldType.IsPrimaryFilter = f.Type.Relationship.IsPrimaryFilter;
                    newFieldType.DisplayInColumn = f.Type.Relationship.DisplayInColumn;
                    if (f.Type.Relationship.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.Relationship.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.Relationship.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.Relationship.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.Relationship.Search.DisplayOrder;
                    }
                }
                else if (f.Type.Text != null)
                {
                    newFieldType.Type = DataType.Text.ToString();
                    newFieldType.ColumnOrder = f.Type.Text.ColumnOrder.HasValue ? f.Type.Text.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Text.ColumnWidth;
                    newFieldType.DefaultValue = f.Type.Text.DefaultValue;
                    newFieldType.DefaultFormattedValue = newFieldType.DefaultValue;
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
                    newFieldType.DisplayInColumn = f.Type.Text.DisplayInColumn;

                    if (f.Type.Text.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Text.Validation.IsRequired;
                        newFieldType.ValidationDescription = f.Type.Text.Validation.Message;
                        newFieldType.MaximumLength = f.Type.Text.Validation.MaximumLength;
                        newFieldType.MinimumLength = f.Type.Text.Validation.MinimumLength;
                        newFieldType.Pattern = f.Type.Text.Validation.Pattern;
                    }
                    if (f.Type.Text.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.Text.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.Text.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.Text.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.Text.Search.DisplayOrder;
                    }

                }
                else if (f.Type.Tag != null)
                {
                    newFieldType.Type = DataType.Tag.ToString();
                    newFieldType.ColumnOrder = f.Type.Tag.ColumnOrder.HasValue ? f.Type.Tag.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Tag.ColumnWidth;
                    if (f.Type.Tag.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Tag.Description.Display;
                    }
                    newFieldType.IsDisplayable = true;
                    newFieldType.IsEditable = false;
                    newFieldType.IsListable = f.Type.Tag.IsListable;
                    newFieldType.IsPartOfKey = false;
                    newFieldType.ShowIfEmpty = true;
                    newFieldType.SortOrder = f.Type.Tag.SortOrder;
                    newFieldType.IsPrimaryFilter = f.Type.Tag.IsPrimaryFilter;
                }
                else if (f.Type.Counter != null)
                {
                    newFieldType.Type = DataType.Counter.ToString();
                    newFieldType.ColumnOrder = f.Type.Counter.ColumnOrder.HasValue ? f.Type.Counter.ColumnOrder.Value : ++maxColumnIndex;
                    newFieldType.ColumnWidth = f.Type.Counter.ColumnWidth;
                    if (f.Type.Counter.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Counter.Description.Display;
                        newFieldType.FormDescription = f.Type.Counter.Description.Form;
                    }

                    newFieldType.IsDisplayable = f.Type.Counter.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Counter.IsEditable;
                    newFieldType.IsListable = f.Type.Counter.IsListable;
                    newFieldType.IsPartOfKey = f.Type.Counter.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.Counter.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Counter.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Counter.SortOrder;
                    newFieldType.CounterPrefix = f.Type.Counter.CounterPrefix;
                    newFieldType.CounterInitialIndex = f.Type.Counter.CounterInitialIndex;
                    newFieldType.DisplayInColumn = f.Type.Counter.DisplayInColumn;


                    if (f.Type.Counter.Search != null)
                    {
                        newFieldType.SearchAddToResult = f.Type.Counter.Search.AddToResult;
                        newFieldType.SearchPrefix = f.Type.Counter.Search.Prefix;
                        newFieldType.SearchSuffix = f.Type.Counter.Search.Suffix;
                        newFieldType.SearchDisplayOrder = f.Type.Counter.Search.DisplayOrder;
                    }
                }
                else
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "No valid type defined", $"You have not included a valid type for the field type [{f.Name}].");
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
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field conversion error", $"You may not convert field {newFieldType.Name} from a {currentFieldType.Type} to a {newFieldType.Type} or a field with the same name and different type may already exist.");
                    }

                    currentFieldType.AllowAllLabel = newFieldType.AllowAllLabel;
                    currentFieldType.AllowAllValue = newFieldType.AllowAllValue;
                    currentFieldType.AllowMultipleValues = newFieldType.AllowMultipleValues;
                    currentFieldType.Category = newFieldType.Category;
                    currentFieldType.ColumnOrder = newFieldType.ColumnOrder;
                    currentFieldType.ColumnWidth = newFieldType.ColumnWidth;
                    currentFieldType.DefaultValue = newFieldType.DefaultValue;
                    currentFieldType.DefaultFormattedValue = newFieldType.DefaultFormattedValue;
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
                    currentFieldType.Definition = newFieldType.Definition;
                    currentFieldType.UpdatedBy = Company.CurrentResourceID;
                    currentFieldType.SearchAddToResult = newFieldType.SearchAddToResult;
                    currentFieldType.SearchPrefix = newFieldType.SearchPrefix;
                    currentFieldType.SearchSuffix = newFieldType.SearchSuffix;
                    currentFieldType.SearchDisplayOrder = newFieldType.SearchDisplayOrder;

                    currentFieldType.CounterPrefix = newFieldType.CounterPrefix;
                    currentFieldType.CounterInitialIndex = newFieldType.CounterInitialIndex;

                    currentFieldType.DisplayInColumn = newFieldType.DisplayInColumn;

                    fieldTypeNamesToDelete.Add(f.Name);
                }

            };

            if (model.RelationshipTypeUid != null)
            {
                newFieldTypes.ForEach(x => x.IsPartOfKey = false);
            }

            if (model.ActionTypeUid.HasValue)
            {
                var action = Company.IssueTypes.FirstOrDefault(x => x.uid == model.ActionTypeUid.Value);
                action.UpdatedBy = Company.CurrentResourceID;
                action.UpdatedOn = DateTime.UtcNow;

            }
            if (model.RelationshipTypeUid.HasValue)
            {
                var intersectType = Company.IntersectTypes.FirstOrDefault(x => x.uid == model.RelationshipTypeUid.Value);
                intersectType.UpdatedBy = Company.CurrentResourceID;
                intersectType.UpdatedOn = DateTime.UtcNow;

            }
            if (model.AssetTypeUid.HasValue)
            {
                var assetType = Company.AssetTypes.FirstOrDefault(x => x.uid == model.AssetTypeUid.Value);
                assetType.UpdatedBy = Company.CurrentResourceID;
                assetType.UpdatedOn = DateTime.UtcNow;
            }

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
                Company.Query<int>("delete FieldType where Object = @t and ObjectID = @tid", new { t = typeIdentifierInfoModel.Object, tid = typeIdentifierInfoModel.ObjectID }).FirstOrDefault();
                Company.FieldTypes.AddRange(newFieldTypes);
            }

            Company.SaveChanges();

            var newKeyFields = string.Join("|",
                Company.Filter<FieldType>(f => f.Object == typeIdentifierInfoModel.Object && f.ObjectID == typeIdentifierInfoModel.ObjectID && f.IsPartOfKey).Select(f => f.ID).OrderBy(f => f)
            );

            if (!newKeyFields.Equals(existingKeyFields))
            {
                // Key fields have changed. You need to update the graph for this asset type.
                if (model.AssetTypeUid.HasValue)
                {
                    Company.SendGraphAssetTypeEvent(model.AssetTypeUid.Value);
                }
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        public bool HasExistingItems(TypeIdentifierInfoModel typeIdentifierInfoModel)
        {
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

            return anyExistingItems;
        }

        public bool hasResponsibilityUsingField(TypeIdentifierInfoModel typeIdentifierInfoModel, List<FieldType> fieldTypes)
        {
            var anyResponsibilityUsingField = false;

            var rules = Company.ResponsibilityTypeRelationRules.Where(x => x.Object == typeIdentifierInfoModel.Object && x.ObjectID == typeIdentifierInfoModel.ObjectID);
            foreach (var rule in rules)
            {
                rule.SetDefinitionFromRaw();
                anyResponsibilityUsingField = rule.StructuredDefinition?.When != null && rule.StructuredDefinition.When.Any(x => fieldTypes.Any(f => f.ID == x.FieldTypeID));
                if (anyResponsibilityUsingField)
                {
                    break;
                }
            }
            return anyResponsibilityUsingField;
        }



        public void DeleteFields(List<FieldType> currentFieldTypes, List<string> fieldNamesToDelete)
        {
            var fieldsRemoved = false;
            bool shouldRefreshPath = false;
            int? assetTypeID = null;
            var impactedMeasureVersions = new List<Guid>();
            bool? assetTypeHasScoringAllocation = null;
            List<FieldType> deletedFieldTypes = new List<FieldType>();

            currentFieldTypes.ForEach(c =>
            {
                assetTypeID = c.AssetTypeID;
                if (!assetTypeHasScoringAllocation.HasValue)
                {
                    assetTypeHasScoringAllocation = Company.Query<bool>("select cast(iif(count(1)>0,1,0) as bit) from metrics.Allocation A inner join AssetType T on T.Uid = A.AssetTypeUid and T.ID = @assetTypeID", new { assetTypeID }).Single();
                }
                if (fieldNamesToDelete.Contains(c.Name))
                {
                    if (c.IsPartOfKey)
                    {
                        shouldRefreshPath = true;
                    }
                    if (assetTypeHasScoringAllocation.Value)
                    {
                        var impacted = Company.GetImpactedMeasureVersionsBy(MetricGovernanceCheckType.Field, c.ID);
                        impactedMeasureVersions.AddRange(impacted);
                    }
                    deletedFieldTypes.Add(c);
                    fieldsRemoved = true;
                }
            });

            if (fieldsRemoved)
            {
                foreach (var ft in deletedFieldTypes)
                {
                    if (ft.Type == DataType.Counter.ToString())
                    {
                        Company.Connection.Execute("delete from dbo.FieldCounterValue where FieldTypeId = @fieldTypeId", new { fieldTypeId = ft.ID });
                    }
                    else
                    {
                        Company.Connection.Execute("update field set updatedby = @CurrentResourceID where FieldTypeId = @fieldTypeId", new { fieldTypeId = ft.ID, Company.CurrentResourceID });
                    }

                    Company.FieldTypes.Remove(ft);
                    Company.SaveChanges();
                }


            }
            if (shouldRefreshPath)
            {
                // Key fields have changed. You need to update the graph IF this is asset type.
                if (assetTypeID.HasValue)
                {
                    var assetType = Company.GetById<AssetType>(assetTypeID.Value);
                    if (assetType != null)
                    {
                        Company.SendGraphAssetTypeEvent(assetType.uid);
                    }
                }
            }

            if (impactedMeasureVersions.Count > 0)
            {
                Company.CreateCheckDependencyRemovedNotificationExecution(impactedMeasureVersions);
            }
        }

        public List<FieldType> GetFieldTypes(TypeIdentifierInfoModel typeIdentifierInfoModel)
        {
            return Company.Filter<FieldType>(f => f.Object == typeIdentifierInfoModel.Object && f.ObjectID == typeIdentifierInfoModel.ObjectID, i => i.FieldTypeLookup).ToList();
        }

        public IEnumerable<string> GetCustomFields(SystemObjects objectType, int objectId)
        {
            return Company.Query<string>(
                @"select distinct  f.FriendlyName   as Name from fieldtype f  
				inner join field f2 on f2.fieldtypeid = f.id 
				 where f.[object] = @objectType and f.objectid = @id ", new { objectType = objectType.ToString(), id = objectId }, ApiTimeout);
        }

        public List<Tuple<string, Guid>> GetFieldInterSetUID(List<FieldType> ExistingFieldType)
        {
            var RetValueList = new List<Tuple<string, Guid>>();

            foreach (var field in ExistingFieldType)
            {
                if (field.Type == DataType.Relationship.ToString() && field.LookupObjectID != null)
                {
                    var intersectType = Company.Filter<IntersectType>(i => i.ID == field.LookupObjectID).SingleOrDefault();
                    if (intersectType != null)
                    {
                        var RetValue = new Tuple<string, Guid>(field.Name, intersectType.uid);
                        RetValueList.Add(RetValue);
                    }
                }
            }

            return RetValueList;
        }

        public List<FieldType> GetFieldDefinitionForComplexLookupFieldType(FieldType fieldType, Guid assetUid, bool forUiFiltering = false)
        {
            if (fieldType.Type == "OwnershipLookup")
            {
                List<string> allowedFields = new List<string>
                        {
                            "ResourceItemUrl","SecurityAssetName","Context","ResourceUid","ResponsibilityTypeName","ResourceName","SecurityAssetUid"
                        };

                return allowedFields.Select(x =>
                    new FieldType
                    {
                        Name = x,
                        Type = DataType.Text.ToString()
                    }).ToList();
            }
            else if (fieldType.Type == "RefListRelationship")
            {
                var fields = new List<FieldType>();

                fields.Add(new FieldType
                {
                    Name = "Code",
                    FriendlyName = "Code",
                    Type = DataType.Text.ToString()
                });

                fields.Add(new FieldType
                {
                    Name = "Color",
                    Type = DataType.Color.ToString()
                });

                fields.AddRange(Company.Query<FieldType>($@"
                    declare @object nvarchar(255)
                    declare @objectId int
                    declare @referenceId int
                    declare @isSubject bit

                    select @object = Object, @objectId = ObjectId from asset where uid = @assetUid

	                select	@isSubject = iif(I.Object = 'ReferenceItemType' and I.ObjectID = 0, 1, 0) 
		                from	IntersectType I 
				                inner join FieldType F on F.LookupObjectType = 'IntersectType' and F.LookupObjectID = I.ID and F.ID = @fieldTypeId;
		
		                if @isSubject = 1
		                begin
			                select	top 1
					                @referenceId = A.ID
			                from	[Intersect] I
					                inner join AssetType A on A.Object = I.Object and A.ObjectID = I.ObjectID and I.Subject = @object and I.Subjectid = @objectId
		                end
		                else
		                begin 
			                select	top 1
					                @referenceId = A.ID
			                from	[Intersect] I
					                inner join AssetType A on A.Object = I.Subject and A.ObjectID = I.SubjectID and I.Object = @object and I.Objectid = @objectId
		                end

                   select * from fieldtype where assettypeid = @referenceid
                        ", new { fieldTypeId = fieldType.ID, assetUid }).ToList());

                return fields;
            }
            else
            {

                var ftl = Company.FieldTypeLookups.FirstOrDefault(x => x.FieldTypeID == fieldType.ID);
                var definition = ftl.ParseComplexLookupDefinition();

                var mappings = definition.GetFieldMapings();
                var fieldTypeIds = definition.Fields.Where(x => !x.FieldTypeName.StartsWith("Related Item.")).Select(x => x.FieldTypeID).Where(x => x > 0).ToList();
                List<FieldType> fields = Company.FieldTypes.Where(x => fieldTypeIds.Contains(x.ID)).AsNoTracking().ToList();
                foreach (var f in mappings)
                {
                    if (f.Value == null)
                    {
                        if (!forUiFiltering)
                        {
                            var ft = new FieldType();
                            ft.Name = f.Key;
                            ft.Type = DataType.Text.ToString();
                            fields.Add(ft);
                            continue;
                        }
                        else
                        {
                            continue;
                        }

                    }
                    if (f.Value.FieldTypeID > 0 && !f.Value.FieldTypeName.StartsWith("Related Item."))
                    {
                        var ft = fields.FirstOrDefault(x => x.ID == f.Value.FieldTypeID);
                        if (ft == null)
                        {
                            continue;
                        }
                        ft.Name = f.Key;
                        ft.FriendlyName = !string.IsNullOrEmpty(f.Value.OverrideDisplayName) ? f.Value.OverrideDisplayName : ft.FriendlyName;
                    }
                    else if (f.Value.FieldTypeName == "DisplayValue")
                    {
                        var ft = new FieldType();
                        ft.Name = f.Key;
                        ft.FriendlyName = !string.IsNullOrEmpty(f.Value.OverrideDisplayName) ? f.Value.OverrideDisplayName : "Display Value";
                        ft.Type = DataType.Text.ToString();
                        fields.Add(ft);
                    }
                    else if (f.Value.FieldTypeName.Contains("_assetPath"))
                    {
                        var ft = new FieldType();
                        ft.Name = f.Key;
                        ft.FriendlyName = !string.IsNullOrEmpty(f.Value.OverrideDisplayName) ? f.Value.OverrideDisplayName : "Asset Path";
                        ft.Type = DataType.Path.ToString();
                        fields.Add(ft);
                    }
                    else if (f.Value.FieldTypeName.StartsWith("Related Item."))
                    {
                        var it = Company.IntersectTypes.FirstOrDefault(x => x.ID == f.Value.FieldTypeID);

                        if (!forUiFiltering)
                        {
                            var ft = new FieldType();

                            ft.Name = f.Key;
                            ft.FriendlyName = f.Value.FieldTypeName;
                            ft.Type = DataType.Relationship.ToString();
                            ft.LookupObjectType = "IntersectType";
                            ft.LookupObjectID = it.ID;
                            fields.Add(ft);
                        }
                        var ft2 = new FieldType();

                        ft2.Name = "$Related:" + it.uid;
                        ft2.FriendlyName = !string.IsNullOrEmpty(f.Value.OverrideDisplayName) ? f.Value.OverrideDisplayName : f.Value.FieldTypeName;
                        ft2.Type = DataType.Relationship.ToString();
                        ft2.LookupObjectType = "IntersectType";
                        ft2.LookupObjectID = it.ID;
                        fields.Add(ft2);
                    }
                    else
                    {
                        var ft = new FieldType();
                        ft.Name = f.Key;
                        ft.FriendlyName = !string.IsNullOrEmpty(f.Value.OverrideDisplayName) ? f.Value.OverrideDisplayName : f.Value.FieldTypeName;
                        ft.Type = DataType.Text.ToString();
                        fields.Add(ft);
                    }
                }

                return fields;
            }
        }

        public async Task<(List<GridColumn>, List<GridField>, List<dynamic>, int, List<dynamic>)> GetComplexRelationLookupGrid(FieldTypeLookup ftl, List<FieldType> fields, DynamicParameters dbArgs, string simpleFilter, string advancedFilter, string orderBy = "", string direction = "asc", bool countOnly = false)
        {
            string orderByClause = "order by 1";
            var Columns = new List<GridColumn>();
            var Fields = new List<GridField>();

            var definition = ftl.ParseComplexLookupDefinition();
            var maps = definition.GetFieldMapings();
            List<string> selects = new List<string>();
            List<string> wheres = new List<string>();

            string sql = ComplexFieldsHelper.GetComplexRelationLookupSQL(definition, dbArgs, fields, selects);
            string countSql = ComplexFieldsHelper.GetComplexRelationLookupSQL(definition, dbArgs, fields, new List<string>(), isCountQuery: true);

            (Columns, Fields) = ComplexFieldsHelper.GetComplexRelationLookupFieldsAndColumns(fields, definition);

            List<string> defaultFilters = new List<string>();
            List<string> simpleFilters = new List<string>();

            foreach (var field in Fields.Where(x => !string.IsNullOrEmpty(x.defaultFilter)))
            {
                defaultFilters.Add($"({field.apiName} ct '{HttpUtility.UrlEncode(field.defaultFilter)}')");
            }

            if (defaultFilters.Count > 0)
            {
                var defFilters = $"({string.Join(" and ", defaultFilters)})";
                advancedFilter = string.IsNullOrEmpty(advancedFilter) ? defFilters : advancedFilter + " and " + defFilters;
            }

            if (!string.IsNullOrEmpty(simpleFilter))
            {
                foreach (var f in Fields.Where(x => !string.IsNullOrEmpty(x.apiName)))
                {
                    simpleFilters.Add($"({f.apiName} ct '{HttpUtility.UrlEncode(simpleFilter)}')");
                }
            }

            if (simpleFilters.Count > 0)
            {
                var sFilter = $"({string.Join(" or ", simpleFilters)})";
                advancedFilter = string.IsNullOrEmpty(advancedFilter) ? sFilter : advancedFilter + " and " + sFilter;
            }

            if (!string.IsNullOrEmpty(advancedFilter))
            {
                var filterDataProvider = new FilterDataProvider(this.Company);
                var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.ComplexLookupField, false, false, true);
                filterExpressionParser.LoadFieldTypes(fields, selects);

                Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                wheres.Add(filterExpressionParser.Parse(advancedFilter, out sqlParams, out _));

                foreach (var p in sqlParams)
                {
                    dbArgs.Add(p.Key, p.Value);
                }
            }

            var sortFields = Fields.Where(x => x.sortOrder > 0).OrderBy(x => x.sortOrder).ToList();
            if (string.IsNullOrEmpty(orderBy) && sortFields.Count > 0)
            {
                List<int> idxs = new List<int>();

                foreach (var item in sortFields)
                {
                    idxs.Add(selects.FindIndex(x => x.ToLowerInvariant().Contains(item.apiName.ToLowerInvariant())));
                }

                orderByClause = "order by " + string.Join(",", idxs);
            }
            else
            {
                var el = selects.Where(x => x != null && x.ToLowerInvariant().Contains(orderBy.ToLowerInvariant())).FirstOrDefault();
                var index = selects.IndexOf(el);
                if (index > 0)
                {
                    orderByClause = "order by " + (index + 1);
                }
            }

            var permissionSQL = "";
            if (!Company.CurrentResourceIsAdmin)
            {
                permissionSQL = $@"	
                                    declare @hasPermission bit = 1

                                    declare @relations table (
		                            RowNumber int identity, RN varchar(3), 
		                            IntersectTypeUid uniqueidentifier, AssetTypeUid uniqueidentifier, RelationType int, Direction int, 
		                            IntersectTypeID int, Object varchar(50), ObjectID int,
		                            FieldCount int null,
		                            AssetTypeId int)

        		                    insert into @relations (IntersectTypeUid, AssetTypeUid, RelationType, Direction, FieldCount)
			                        select	R.*,
					                        0
			                        from	FieldTypeLookup O
					                        cross apply OPENJSON(O.[Definition], N'lax $.Relations') with (
						                        IntersectTypeUid uniqueidentifier, 
						                        AssetTypeUid uniqueidentifier,
						                        RelationType int, 
						                        Direction int
					                        ) R
			                        where	O.FieldTypeID = @fieldTypeId;

                                    update	R 
		                            set		R.RN = cast(R.RowNumber as varchar(5)),
				                            R.IntersectTypeID = I.ID,
				                            R.Object = A.Object,
				                            R.ObjectID = A.ObjectID,
				                            R.AssetTypeId = A.ID
		                            from	@relations R
		                            left join IntersectType I on I.Uid = R.IntersectTypeUid
		                            left join AssetType A on A.Uid = R.AssetTypeUid

		                            drop table if exists #AssetPermission;
		                            Create table #AssetPermission (AssetID bigint);
		                            Create nonclustered index Ix_PermissAsset_temp on #AssetPermission(AssetID);

		                            insert into #AssetPermission
		                            select P.AssetID
		                            from @relations r
		                            cross apply  dbo.UserAssetPermissions(@resourceId, r.AssetTypeID) P
		                            where (PermissionsBitMask & 1) = 0;

		                        -- If resource can't read asset type of one or more hops, there should be no result. Add impossible condition.
		                        if exists (select 1 from dbo.AssetTypesUserCantRead(@resourceId) u where u.AssetTypeID in (select AssetTypeID from @relations))
		                        begin
			                        set @hasPermission = 0;
		                        end";


                wheres.Add("not exists (select 1 from #AssetPermission p where H1.ID = p.AssetID)");
                wheres.Add("@hasPermission = 1");
            }

            var itemsSQL = $@"{permissionSQL}
                            {sql}  
                            {(wheres.Count == 0 ? "" : "where " + string.Join(" and ", wheres))}
                            {orderByClause} {direction}
                            offset((@pageNum - 1) * @pageSize) rows fetch next @pageSize rows only";
            var countSQL = $@"{countSql}
                              {(wheres.Count == 0 ? "" : "where " + string.Join(" and ", wheres))}";

            if (countOnly)
            {
                itemsSQL = permissionSQL;
            }

            var reader = await Company.QueryMultipleAsync(
                $"{itemsSQL}; {countSQL}", dbArgs);

            var Values = new List<dynamic>();
            if (!countOnly)
            {
                Values = reader.Read<dynamic>().ToList();
            }
            var count = reader.Read<int>().FirstOrDefault();
            var scoringInfo = new List<dynamic>();

            var scoreFields = fields.Where(x => x.Type == "Score").Select(x => x.ID).ToList();
            if (scoreFields.Any())
            {
                scoringInfo = (await Company.QueryAsync($@"select ft.id AS FieldTypeId, ma.ScoreType, ma.LowerThreshold, ma.UpperThreshold from 
			                 FieldType ft 
			                inner join AssetType at on ft.object = at.object and ft.objectid = at.objectid
			                inner join metrics.Allocation ma on ma.AssetTypeUid = at.uid  and ma.ScoreType = ft.ScoreType
		                where ft.Type = 'Score' and ft.id in @scoreFields", new { scoreFields })).ToList();
            }

            return (Columns, Fields, Values, count, scoringInfo);
        }

        public async Task<(List<GridColumn>, List<GridField>, List<dynamic>, int)> GetRefListFromRelationshipGrid(List<FieldType> fields, DynamicParameters dbArgs, string simpleFilter, string advancedFilter, string orderBy = "", string direction = "asc", bool countOnly = false)
        {
            string orderByClause = "order by A.Code";
            var Columns = new List<GridColumn>();
            var Fields = new List<GridField>();

            List<string> selects = new List<string>();
            List<string> joins = new List<string>();
            List<string> wheres = new List<string>();

            int assetTypeId = await GetAssetTypeIdForRefListField(dbArgs);

            wheres.Add("A.AssetTypeID = @assetTypeId");
            wheres.Add("not exists(select 1 from dbo.AssetTypesUserCantRead(@resourceid) u where u.AssetTypeID = A.AssetTypeID)");
            dbArgs.Add("assetTypeId", assetTypeId);

            string itemsSQL = ComplexFieldsHelper.GetRefListFromRelSQL(fields, dbArgs, selects, joins, false);
            string countSQL = ComplexFieldsHelper.GetRefListFromRelSQL(fields, dbArgs, selects, joins, true);
            (Columns, Fields) = ComplexFieldsHelper.GetComplexRefListFromRelFieldsAndColumns(fields);

            List<string> simpleFilters = new List<string>();
            if (!string.IsNullOrEmpty(simpleFilter))
            {
                foreach (var f in Fields.Where(x => !string.IsNullOrEmpty(x.apiName)))
                {
                    simpleFilters.Add($"({f.apiName} ct '{HttpUtility.UrlEncode(simpleFilter)}')");
                }
            }

            if (simpleFilters.Count > 0)
            {
                var sFilter = $"({string.Join(" or ", simpleFilters)})";
                advancedFilter = string.IsNullOrEmpty(advancedFilter) ? sFilter : advancedFilter + " and " + sFilter;
            }

            if (!string.IsNullOrEmpty(advancedFilter))
            {
                var filterDataProvider = new FilterDataProvider(this.Company);
                var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.ComplexLookupField, false, false, true);
                filterExpressionParser.LoadFieldTypes(fields, selects);

                Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                wheres.Add(filterExpressionParser.Parse(advancedFilter, out sqlParams, out _));

                foreach (var p in sqlParams)
                {
                    dbArgs.Add(p.Key, p.Value);
                }
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                var el = selects.FirstOrDefault(x => x.ToLowerInvariant().Contains($"[{orderBy.ToLowerInvariant()}]"));
                var idx = selects.IndexOf(el);
                if (idx > 0)
                {
                    orderByClause = "order by " + (idx + 1);
                }
            }

            itemsSQL = $@"{itemsSQL}
                            {(wheres.Count == 0 ? "" : "where " + string.Join(" and ", wheres))}
                            {orderByClause} {direction}
                            offset((@pageNum - 1) * @pageSize) rows fetch next @pageSize rows only";

            countSQL = $@"{countSQL}
                            {(wheres.Count == 0 ? "" : "where " + string.Join(" and ", wheres))}";

            if (countOnly)
            {
                itemsSQL = "";
            }

            var reader = await Company.QueryMultipleAsync(
                $"{itemsSQL}; {countSQL}", dbArgs);

            var Values = new List<dynamic>();
            if (!countOnly)
            {
                Values = reader.Read<dynamic>().ToList();
            }

            var count = reader.Read<int>().FirstOrDefault();

            return (Columns, Fields, Values, count);
        }

        public async Task<(List<GridColumn>, List<GridField>, List<dynamic>, int)> GetOwnershipLookupGrid(FieldTypeLookup ftl, List<FieldType> fields, DynamicParameters dbArgs, string simpleFilter, string advancedFilter, string orderBy = "", string direction = "asc", bool countOnly = false)
        {
            var definition = ftl.ParseOwnershipLookupDefinition();

            List<GridColumn> Columns = new List<GridColumn>();
            List<GridField> Fields = new List<GridField>();

            Columns.Add(new GridColumn { text = "Responsibility", datafield = "ResponsibilityTypeName", columntype = "textbox" });
            Columns.Add(new GridColumn { text = "Assigned User/Group", datafield = "ResourceName", columntype = "preview", uidfield = "SecurityAssetUid", urlfield = "ResourceItemUrl" });
            if (definition.DisplayAssignmentSource)
            {
                Columns.Add(new GridColumn { text = "Via", datafield = "SecurityAssetName", columntype = "preview", uidfield = "SecurityAssetUid" });
                Fields.Add(new GridField { apiName = "SecurityAssetName", name = "SecurityAssetName", type = "preview" });

            }
            Columns.Add(new GridColumn { text = "Context", datafield = "Context", columntype = "textbox" });


            Fields.Add(new GridField { apiName = "ResponsibilityTypeName", name = "ResponsibilityTypeName", type = "string" });
            Fields.Add(new GridField { apiName = "ResourceName", name = "ResourceName", type = "preview" });
            Fields.Add(new GridField { name = "ResourceItemUrl", type = "string" });
            Fields.Add(new GridField { name = "SecurityAssetUid", type = "string" });
            Fields.Add(new GridField { apiName = "Context", name = "Context", type = "html" });


            List<string> selects = new List<string>();
            List<string> wheres = new List<string>();
            string orderByClause = "ORDER BY r.responsibilitytypename ASC,resourcename";

            selects.Add("r.context AS [Context]");
            selects.Add("r.responsibilitytypename AS [ResponsibilityTypeName]");
            selects.Add("SecurityAssetUid AS [SecurityAssetUid]");

            if (definition.ExpandGroupMembership != false)
            {
                selects.Add("'/resource/' + Cast(r.resourceid AS VARCHAR) AS [ResourceItemUrl]");
                selects.Add("r.resourceuid AS [ResourceUid]");
                selects.Add("resourcename as [ResourceName]");
            }
            else
            {
                selects.Add(@"CASE securityassetname
                                                      WHEN resourcename THEN '/resource/' + Cast(r.resourceid AS    VARCHAR)
                                                       ELSE '/group/' + Cast(securityassetid AS VARCHAR)
                                       END AS [ResourceItemUrl]");

                selects.Add(@" CASE securityassetname
                                          WHEN resourcename THEN 'Resource'
                                          ELSE 'Group'
                                       END AS [ResourceObject]");
                selects.Add("securityassetname as [ResourceName]");

                orderByClause = "ORDER BY r.responsibilitytypename ASC,securityassetname";
            }

            if (definition.DisplayAssignmentSource)
            {
                selects.Add("r.SecurityAssetName AS [SecurityAssetName]");
            }

            wheres.Add("r.isvisible = 1");
            wheres.Add("((r.assetid = A.Id) or (r.applytotype = 1 AND r.assettypeid = a.assettypeid))");

            if (definition.ResponsibilityType != null)
            {
                wheres.Add("(r.responsibilitytypeid = @responsibilityTypeId)");
                dbArgs.Add("responsibilityTypeId", definition.ResponsibilityType);
            }

            List<string> simpleFilters = new List<string>();
            if (!string.IsNullOrEmpty(simpleFilter))
            {
                foreach (var f in Fields.Where(x => !string.IsNullOrEmpty(x.apiName)))
                {
                    simpleFilters.Add($"({f.apiName} ct '{HttpUtility.UrlEncode(simpleFilter)}')");
                }
            }

            if (simpleFilters.Count > 0)
            {
                var sFilter = $"({string.Join(" or ", simpleFilters)})";
                advancedFilter = string.IsNullOrEmpty(advancedFilter) ? sFilter : advancedFilter + " and " + sFilter;
            }

            if (!string.IsNullOrEmpty(advancedFilter))
            {
                var filterDataProvider = new FilterDataProvider(this.Company);
                var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.ComplexLookupField, false, false, true);
                filterExpressionParser.LoadFieldTypes(fields, selects);

                Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                wheres.Add(filterExpressionParser.Parse(advancedFilter, out sqlParams, out _));

                foreach (var p in sqlParams)
                {
                    dbArgs.Add(p.Key, p.Value);
                }
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                var el = selects.FirstOrDefault(x => x.ToLowerInvariant().Contains($"[{orderBy.ToLowerInvariant()}]"));
                var idx = selects.IndexOf(el);
                if (idx > 0)
                {
                    orderByClause = "order by " + (idx + 1);
                }
            }

            var itemsSQL = $@"select distinct 
                            {(string.Join(", ", selects))}
                        FROM[dbo].[ResponsibilityDetail] R
                        inner join asset a on a.uid = @assetuid
                        {(wheres.Count == 0 ? "" : "where " + string.Join(" and ", wheres))}
                        {orderByClause} {direction}
                        offset((@pageNum - 1) * @pageSize) rows fetch next @pageSize rows only";


            var countSQL = $@"select distinct 
                            count(*)
                        FROM [dbo].[ResponsibilityDetail] R
                        inner join asset a on a.uid = @assetuid
                        {(wheres.Count == 0 ? "" : "where " + string.Join(" and ", wheres))}";


            if (countOnly)
            {
                itemsSQL = "";
            }

            var reader = await Company.QueryMultipleAsync(
                $"{itemsSQL}; {countSQL}", dbArgs);

            var Values = new List<dynamic>();
            if (!countOnly)
            {
                Values = reader.Read<dynamic>().ToList();
            }
            var count = reader.Read<int>().FirstOrDefault();
            return (Columns, Fields, Values, count);
        }

        private async Task<int> GetAssetTypeIdForRefListField(DynamicParameters dbArgs)
        {
            return (await Company.QueryAsync<int>($@"declare @isSubject bit,
				                        @referenceItemTypeID int
		                        select	@isSubject = iif(I.Object = 'ReferenceItemType' and I.ObjectID = 0, 1, 0) 
		                        from	IntersectType I 
				                        inner join FieldType F on F.LookupObjectType = 'IntersectType' and F.LookupObjectID = I.ID and F.ID = @fieldTypeId;
		
		                        if @isSubject = 1
		                        begin
			                        select	top 1
					                        @referenceItemTypeID = A.ID
			                        from	[Intersect] I
					                        inner join AssetType A on A.Object = I.Object and A.ObjectID = I.ObjectID and I.Subject = @object and I.Subjectid = @objectId
		                        end
		                        else
		                        begin 
			                        select	top 1
					                        @referenceItemTypeID = A.ID
			                        from	[Intersect] I
					                        inner join AssetType A on A.Object = I.Subject and A.ObjectID = I.SubjectID and I.Object = @object and I.Objectid = @objectId
		                        end
		                        select @referenceItemTypeID", dbArgs)).FirstOrDefault();
        }

    }
}
