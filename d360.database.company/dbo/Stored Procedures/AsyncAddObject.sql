CREATE procedure [dbo].[AsyncAddObject]
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
		
		exec [cache].[SynchronizeObjectDetails] @Object, @ObjectID

		exec [utility].[AddAuditEntry] @ParentObject, @ParentObjectID, @ResourceID, @date, 'Created', @Object, @ObjectID

		if @Object in ('AttributeTypeRelation', 'AttributeTypeRelation', 'ResponsibilityTypeRelation', 'ResponsibilityType')
		begin
			exec utility.CalculateStatistics
		end
		else
		begin
			exec utility.CalculateStatistics @Object, @ObjectID
		end

		if @Object = 'Intersect'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
		end

		if @Object = 'Responsibility'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
		end

		if @Object = 'Artifact'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @Object, @ObjectID 
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