CREATE PROCEDURE [utility].[PromoteFusionAttributeLookups]	 
AS
BEGIN
	SET NOCOUNT ON;

	declare @currentID int = 0,
			@maxID int = 0;
	

	IF OBJECT_ID('tempdb..#fieldValues') IS NOT NULL
		DROP TABLE #fieldValues;

	create table #fieldValues (
		ObjectType varchar(50), 
		ObjectID int, 
		FieldTypeID int, 
		Value int
	);


	insert into #fieldValues
		select 
			fap.ObjectType as ObjectType,
			fap.ObjectID as ObjectID,
			fusLook.FieldTypeID as FieldTypeID,						
			max(fap.fusionattributeid) as Value						
		from [dbo].[FusionAttributePromotionRule] pr
		inner join [dbo].[fieldtype] ft on (ft.[objectid] = pr.PromotionObjectID and ft.[object] = pr.PromotionObjectType)
		inner join [dbo].[FieldTypeFusionLookupDefinition] fusLook  on (ft.id = fusLook.fieldTypeid)
		inner join fusionattribute fa on (fa.fusionattributetypeid = pr.ObjectID and fa.fusionAttributetypeid = fusLook.SourceFusionAttributeTypeID)
		inner join fusionattributepromotion fap on (fa.id = fap.fusionattributeid)
		where pr.[enabled] = 1 and fap.ObjectType != 'Intersect' group by fap.ObjectType, fap.ObjectID, fusLook.FieldTypeID
	
		
	If EXISTS (SELECT 1 FROM #fieldValues)		
	begin
		--debug shows values 
		--select * from #fieldValues

		merge	Field as T
				using	(
					select f.ObjectType as ObjectType,
							f.ObjectID as ObjectID,
							f.FieldTypeID as FieldTypeID,
							f.Value as Value
					from #fieldValues f inner join dbo.FieldType ft on (ft.ID = f.FieldTypeID)
				) as S
				on		T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
				when	matched then
					update set T.Value = S.Value
				when	not matched then
					insert (ObjectTYpe, OBjectID, FieldTypeID, Value)
					values (S.ObjectType, S.ObjectID, S.FieldTypeID, S.Value);
	end

END

--exec [utility].[PromoteFusionAttributeLookups]	 