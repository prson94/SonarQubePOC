namespace d360.core.queue
{
	public enum AssetTypeChangeAction
	{ 
		Removal = 1,
		FieldAddition = 2,
		FieldRemoval = 3,
	}

	public class AssetTypeChangeMessage: QueueObject
	{
        public int AssetTypeId { get; set; }
		public AssetTypeChangeAction Action { get; set; }
		public int? FieldTypeId { get; set; }
	}

}
