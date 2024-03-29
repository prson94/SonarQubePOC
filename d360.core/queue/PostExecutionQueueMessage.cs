using System;

public enum PostExecutionQueueMessageAction
{
	History = 1,
	Indexing,
	Scoring,
	Workflow,
	UpdateAssetLookupValues
}

public enum HistoryType
{
	AssetType
}

public class PostExecutionQueueMessage
{
	public PostExecutionQueueMessageAction Action { get; set; }
	public int ExecutionId { get; set; }
	public int CompanyID { get; set; }
	public Guid? uid { get; set; }
	public HistoryType? HistoryType { get; set; }
}