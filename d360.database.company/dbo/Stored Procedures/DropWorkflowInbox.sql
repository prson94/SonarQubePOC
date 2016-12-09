CREATE PROCEDURE [DropWorkflowInbox] 
		@processId uniqueidentifier
	AS
	BEGIN
		BEGIN TRAN	
		DELETE FROM [workflow].WorkflowInbox WHERE ProcessId = @processId	
		COMMIT TRAN
	END