using System;
using System.Collections;
using System.Collections.Generic;

namespace repositories.dis.models
{
	public class XrefModel
	{
		public string type { get; set; }
		public string id { get; set; }
	}

	public class GetPropertyModel
	{
		public string name { get; set; }
		public string value { get; set; }
	}

	public class GetTagModel
	{

	}

	public class GetPathModel
	{
		public string assetId { get; set; }
		public string assetTypeId { get; set; }
		public string assetTypeName { get; set; }
		public ICollection<string> segments { get; set; }
	}

	public class GetAssetModel
	{
		public string id { get; set; }
		public string assetTypeId { get; set; }
		public ICollection<XrefModel> xrefs { get; set; }
		public string @class { get; set; }
		public ICollection<GetPropertyModel> properties { get; set; }
		public ICollection<GetTagModel> tags { get; set; }
		public ICollection<GetPathModel> path { get; set; }
		public string createdBy { get; set; }
		public DateTime	createdAt { get; set; }
		public string updatedBy { get; set; }
		public DateTime updatedAt { get; set; }
	}
}
