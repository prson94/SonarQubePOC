CREATE view [dbo].[AssetDetail]
as
	select	A.ID,
			A.AssetTypeID,
			A.State,
			A.Object,
			A.ObjectID,
			A.SourceID,
			D.DisplayValue,
			K.KeyHash,
			F.FieldHash,
			A.CreatedOn,
			A.CreatedBy,
			A.UpdatedOn,
			A.UpdatedBy,
			A.AssetTypeClass,
			A.AssetTypeDescription,
			A.TypeName,
			A.Type,
			A.TypeID,
			A.BackColor,
			A.ForeColor,
			A.Icon
	from	AssetWithType A
			outer apply dbo.GetAssetDisplayValueById(A.ID) D	--left join GetAssetDisplayValue() D on D.ID = A.ID
			left join GetAssetKeyHash() K on K.ID = A.ID
			left join GetAssetFieldHash() F on F.ID = A.ID
GO