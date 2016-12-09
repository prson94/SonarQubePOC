CREATE PROCEDURE [DropWorkflowProcess] 
		@id uniqueidentifier
	AS
	BEGIN
		BEGIN TRAN
	
		DELETE FROM [workflow].WorkflowProcessInstance WHERE Id = @id
		DELETE FROM [workflow].WorkflowProcessInstanceStatus WHERE Id = @id
		DELETE FROM [workflow].WorkflowProcessInstancePersistence  WHERE ProcessId = @id
	
		COMMIT TRAN
	END