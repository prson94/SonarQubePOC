CREATE FUNCTION [utility].[GetObjectHash]
(
--declare
	@Object varchar(50),-- = 'Artifact',
	@ObjectID int,-- = 733,
	@ObjectTypeID int,-- = 2,
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
				from		Field F
							inner join FieldType FT on FT.ID = F.FieldTypeID 
													and F.ObjectType = @Object and F.ObjectID = @ObjectID 
													and FT.Object = @Object + 'Type' and FT.ObjectID = @ObjectTypeID
													and ( (@KeyFieldOnly = 1 and FT.IsPartOfKey = @KeyFieldOnly) or (@KeyFieldOnly = 0 and 1=1) )
				order by	FT.ID
				) A

	return @hash
END