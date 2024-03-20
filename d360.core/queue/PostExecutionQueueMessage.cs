using System;

public enum PostExecutionQueueMessageAction
{
	History = 1,
	Indexing,
	Scoring,
	Workflow,
	UpdateAssetLookupValues
}

public class PostExecutionQueueMessage
{
	public PostExecutionQueueMessageAction Action { get; set; }
	public int ExecutionId { get; set; }
	public int CompanyID { get; set; }
}