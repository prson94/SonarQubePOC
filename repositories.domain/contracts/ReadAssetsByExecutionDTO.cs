using System;

namespace repositories.domain.contracts
{
	public class ReadAssetsByExecutionDTO
	{
		public long Id { get; set; }
		public int AssetTypeId { get; set; }
		public Guid Uid { get; set; }
		public string Object { get; set; }
		public int ObjectID { get; set; }
		public string ObjectType { get; set; }
		public int ObjectTypeID { get; set; }
		public bool IsNew { get; set; }
	}
}
