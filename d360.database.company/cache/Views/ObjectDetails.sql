
CREATE view [cache].[ObjectDetails]
as
select
	T.Object,
	T.ObjectID,
	T.Name,
	T.Name as TextPath,
	cast(null as nvarchar) as Description,		
	T.Url,
	T.Url as NgUrl,
	cast(null as varchar) as Parent,
	cast(null as int) as ParentID,
	cast(null as nvarchar) as ParentName,
	T.ObjectType,
	T.ObjectTypeID,
	T.ObjectTypeName,
	T.IconBackColor,
	T.IconForeColor,
	T.IconText
from
	( select	A.Object as Object,
		A.ObjectID as ObjectID,
		utility.GetAssetDisplayValue(A.ID) as Name,						
		AUrl.[Url] as [Url],
		AST.Object as ObjectType,
		AST.ObjectID as ObjectTypeID,
		AST.Name as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
	from	AssetType AST
		left join ObjectStyle S on S.ObjectType = AST.Object and S.ObjectID = AST.ObjectID
		left join Asset A on A.AssetTypeID = AST.ID
		cross apply utility.GenerateObjectUrls(A.[Object], AST.ObjectID, A.ObjectID) AUrl
			) T		
union -- types
select
	T_t.Object,
	T_t.ObjectID,
	T_t.Name,
	T_t.Name as TextPath,
	cast(null as nvarchar) as Description,		
	T_t.Url,
	T_t.Url as NgUrl,
	cast(null as varchar) as Parent,
	cast(null as int) as ParentID,
	cast(null as nvarchar) as ParentName,
	T_t.ObjectType,
	T_t.ObjectTypeID,
	T_t.ObjectTypeName,
	T_t.IconBackColor,
	T_t.IconForeColor,
	T_t.IconText
from
( select	AST.Object as Object,
		AST.ObjectID as ObjectID,
		AST.Name as Name,						
		AUrl.[Url] as [Url],
		AST.Object as ObjectType,
		AST.ObjectID as ObjectTypeID,
		null as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
	from	AssetType AST
		left join ObjectStyle S on S.ObjectType = AST.Object and S.ObjectID = AST.ObjectID		
		cross apply utility.GenerateObjectUrls(AST.[Object], AST.ObjectID, AST.ObjectID) AUrl
			) T_t
union -- intersects
select	'Intersect' as Object,
		I.ID as ObjectID,
		IName.Name as Name,
		IName.Name as TextPath,		
		cast(null as nvarchar) as Description,
		null as Url,
		null as NgUrl,
		cast(null as varchar) as Parent,
		cast(null as int) as ParentID,
		cast(null as nvarchar) as ParentName,
		'IntersectType' as ObjectType,
		IT.ID as ObjectTypeID,
		ITypeName.Name as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
from	IntersectType IT		
		inner join [Intersect] I on I.IntersectTypeID = IT.ID		
		left join ObjectStyle S on S.ObjectType = 'IntersectType' and S.ObjectID = IT.ID		
		cross apply utility.GetIntersectNames(I.ID) IName	
		cross apply utility.GetIntersectTypeNames(IT.ID) ITypeName

union -- intersect types
select	'IntersectType' as Object,
		I_T.ID as ObjectID,
		ITypeName.Name as Name,
		ITypeName.Name as TextPath,		
		cast(null as nvarchar) as Description,
		null as Url,
		null as NgUrl,
		cast(null as varchar) as Parent,
		cast(null as int) as ParentID,
		cast(null as nvarchar) as ParentName,
		'IntersectType' as ObjectType,
		0 as ObjectTypeID,
		null as ObjectTypeName,
		coalesce(S.IconBackColor, '#000') as IconBackColor,
		coalesce(S.IconForeColor, '#fff') as IconForeColor,
		coalesce(S.IconText, 'leaf') as IconText
from	IntersectType I_T				
		left join ObjectStyle S on S.ObjectType = 'IntersectType' and S.ObjectID = I_T.ID				
		cross apply utility.GetIntersectTypeNames(I_T.ID) ITypeName

GO