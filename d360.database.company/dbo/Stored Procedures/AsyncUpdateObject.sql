CREATE procedure [dbo].[AsyncUpdateObject]
	@Object varchar(50),
	@ObjectID int,
	@ParentObject varchar(50),
	@ParentObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	declare @trans varchar(25) = 'Trans',
			@current int = 1,
			@max int,
			@date datetime = getutcdate()

	begin try
		begin transaction @trans
		
		--exec [cache].[SynchronizeObjectDetails] @Object, @ObjectID
		
		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID]) values ('ObjectIndex', 'U', @Object, @ObjectID)
		exec [utility].[AddAuditEntry] @ParentObject, @ParentObjectID, @ResourceID, @date, 'Updated', @Object, @ObjectID

		if @Object = 'Artifact'
		begin
			with h as	(
						select	ID,
								ParentID
						from	Artifact
						where	ID = @ObjectID
						union all
						select	A.ID,
								A.ParentID
						from	Artifact A
								inner join h P on P.ID = A.ParentID
						)
			update	T
			set		T.TextPath = utility.GetBreadcrumbStringWrapper(@Object, S.ID, '/')
			from	Artifact T
					inner join h S on S.ID = T.ID;
		end

		if @Object in ('AttributeTypeRelation', 'AttributeTypeRelation', 'ResponsibilityTypeRelation', 'ResponsibilityType')
		begin
			exec utility.CalculateStatistics
		end
		else
		begin
			exec utility.CalculateStatistics @Object, @ObjectID
		end

		if @Object = 'Responsibility'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
		end

		if @Object = 'Taxonomy'
		begin
			with h as	(
						select	ID,
								ParentID
						from	Taxonomy
						where	ID = @ObjectID
						union all
						select	A.ID,
								A.ParentID
						from	Taxonomy A
								inner join h P on P.ID = A.ParentID
						)
			update	T
			set		T.TextPath = utility.GetBreadcrumbStringWrapper(@Object, S.ID, '/')
			from	Taxonomy T
					inner join h S on S.ID = T.ID;

			UPDATE	F
			set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
			FROM	Field F
					inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = 'Taxonomy' 
					inner join Taxonomy A on A.ID = @ObjectID and A.TaxonomyTypeID = FT.LookupObjectID

			exec [cache].[SynchronizeResponsibilities]
		end

		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

		rollback transaction @trans
	end catch
end