using d360.core;

public enum PostExecutionQueueMessageAction
{
	History = 1,
	Indexing,
	Scoring,
	Workflow,
	UpdateAssetLookupValues,
	UpdateAssetPaths
}

public enum ChangeLogType
{
	Created, Updated, Removed
}

public class PostExecutionQueueMessage
{
	public PostExecutionQueueMessageAction Action { get; set; }
	public int ExecutionId { get; set; }
	public int CompanyID { get; set; }
	public ObjectInfo ObjectInfo { get; set; }
}

public class ObjectInfo
{
	public string Object { get; set; }
	public long ObjectId { get; set; }
	public int? AssetTypeId { get; set; }
	public int? IssueTypeId { get; set; }
	public int? IntersectTypeId { get; set; }
	public int ResourceId { get; set; }
	public ChangeLogType ChangeType { get; set; }
}