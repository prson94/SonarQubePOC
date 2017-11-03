create VIEW [dbo].[ResponsibilityDetails] WITH SCHEMABINDING 
AS 
select	O.AssetID,
		A.Object,
		A.ObjectID,
		T.Object as Type,
		T.ObjectID as TypeID,
		R.Name as RuleName,
		O.ResponsibilityTypeID,
		RT.Name as ResponsibilityTypeName,
		GrRe.FirstName,
		GrRe.LastName,
		case O.SecurityAsset
			when 'G' then ReGr.ResourceID
			when 'O' then OrRe.ResourceID
			when 'R' then O.SecurityAssetID
			else null
		end as ResourceID,
		O.SecurityAsset,
		O.SecurityAssetID,
		case O.SecurityAsset
			when 'G' then Gr.Name
			when 'O' then Org.Name
			when 'R' then GrRe.LastName + ', ' + GrRe.FirstName
			else null
		end as SecurityAssetName,
		O.Overriden,
		O.OverrideItemID
from	dbo.ResponsibilityTypeRelationItem O
		inner join dbo.Asset A on A.ID = O.AssetID
		inner join dbo.AssetType T on T.ID = A.AssetTypeID
		left join dbo.ResponsibilityTypeRelationRule R on R.ID = O.RuleID
		inner join dbo.ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
		left join dbo.OrganizationResource OrRe on O.SecurityAsset = 'O' and OrRe.OrganizationID = O.SecurityAssetID
		left join dbo.Organization Org on O.SecurityAsset = 'O' and Org.ID = OrRe.OrganizationID
		left join dbo.ResourceGroup ReGr on O.SecurityAsset = 'G' and ReGr.GroupID = O.SecurityAssetID
		left join dbo.[Group] Gr on O.SecurityAsset = 'G' and Gr.ID = ReGr.GroupID
		inner join reporting.Global_Resource GrRe on GrRe.ResourceID =	case O.SecurityAsset
																			when 'G' then ReGr.ResourceID
																			when 'O' then OrRe.ResourceID
																			when 'R' then O.SecurityAssetID
																			else null
																		end
where	O.Overriden = 0