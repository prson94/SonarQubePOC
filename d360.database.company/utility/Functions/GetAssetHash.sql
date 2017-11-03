CREATE FUNCTION [utility].[GetAssetHash]
(
--declare
	@ID bigint,-- = 733,
	@KeyFieldOnly bit-- = 1	
)
RETURNS varchar(50)
AS
BEGIN
	declare @hash varchar(50)

	select		@hash = CONVERT(
					varchar(32), 
					SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					2)
	from		(
				select		top 100 percent
							F.FieldTypeID,
							coalesce(F.Value, '') as Value
				from		Asset A
							inner join AssetType T on T.ID = A.AssetTypeID and A.ID = @ID
							inner join Field F on F.ObjectType = A.Object and F.ObjectID = A.ObjectID
							inner join FieldType FT on FT.ID = F.FieldTypeID 
													and FT.Object = T.Object and FT.ObjectID = T.ObjectID
													and ( (@KeyFieldOnly = 1 and FT.IsPartOfKey = @KeyFieldOnly) or (@KeyFieldOnly = 0 and 1=1) )
				order by	FT.ID
				) A

	return @hash
END