CREATE PROCEDURE UpdateObject
	@Object varchar(50),
	@objectID int,
	@ResourceID int
AS
BEGIN
	SET NOCOUNT ON;

	if @Object = 'FusionAttributeType'
	begin
		with S as (
			select  ID,
					ParentID
			from    FusionAttributeType
			where   ID = @ObjectID
			union all
			select	C.ID,
					C.ParentID
			from	FusionAttributeType C
					inner join S on S.ID = C.ParentID
		)
		update	T
		set		T.ScanEnabled = 0,
				T.UpdatedOn = getutcdate(),
				T.UpdatedBy = @ResourceID
		from	FusionAttributeType T
				inner join S on S.ID = T.ID and S.ID <> @ObjectID
	end
END