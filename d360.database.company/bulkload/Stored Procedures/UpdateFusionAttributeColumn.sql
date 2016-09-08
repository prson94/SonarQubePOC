
create procedure bulkload.UpdateFusionAttributeColumn
	@id int,
	@fusionConfigColumn int,
	@fusionAttributeColumn int
as
begin
	set nocount on;
	update	TA
	set		TA.LookupObject = 'FusionAttribute',
			TA.LookupObjectID = S.ID
	from	LoadItemColumn TA
			inner join LoadItemColumn TC on TA.LoadID = TC.LoadID and TA.LoadID = @id and TA.RowIndex = TC.RowIndex and TC.ColumnIndex = @fusionConfigColumn and TA.ColumnIndex = @fusionAttributeColumn
			inner join	(
						select		A.ID,
									C.FusionID,
									C.TextPath 
						from		(
									select		TC.LookupObjectID as FusionID,
												TA.Value as TextPath
									from		LoadItemColumn TA
												inner join LoadItemColumn TC on TA.LoadID = TC.LoadID and TA.LoadID = @id and TA.RowIndex = TC.RowIndex and TC.ColumnIndex = @fusionConfigColumn and TA.ColumnIndex = @fusionAttributeColumn
									where		TC.LookupObject = 'Fusion' and TC.LookupObjectID is not null
									group by	TC.LookupObjectID, TA.Value
									) C
									inner join FusionAttribute A on A.FusionID = C.FusionID and lower(A.TextPath) = lower(C.TextPath)
						) S on S.FusionID = TC.LookupObjectID and S.TextPath = TA.Value
end