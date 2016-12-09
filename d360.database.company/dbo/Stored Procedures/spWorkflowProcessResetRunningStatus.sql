CREATE PROCEDURE [spWorkflowProcessResetRunningStatus]
	AS
	BEGIN
		UPDATE [workflow].[WorkflowProcessInstanceStatus] SET [Status] = 2 WHERE [Status] = 1
	END