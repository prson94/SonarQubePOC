create view [dbo].[AssetWithType]
as
	select	A.ID,
			A.AssetTypeID,
			A.State,
			A.Object,
			A.ObjectID,
			A.SourceID,
			A.CreatedOn,
			A.CreatedBy,
			A.UpdatedOn,
			A.UpdatedBy,
			T.Class as AssetTypeClass,
			T.Description as AssetTypeDescription,
			T.Name as TypeName,
			T.Object as Type,
			T.ObjectID as TypeID,
			coalesce(S.IconBackColor, '#000') as BackColor,
			coalesce(S.IconForeColor, '#fff') as ForeColor,
			coalesce(S.IconText, 'leaf') as Icon
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID
			left join ObjectStyle S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID
GO