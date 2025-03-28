using System;
using System.Collections.Generic;
using System.Text;

namespace d360.core.entities
{
	public class ProcessDiagramExportNameValueModel
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}

	public class ProcessDiagramExportNodeModel
	{
		public decimal StepNo { get; set; }
		public string Name { get; set; }
		public string GovernanceRole { get; set; }
		public string FlowObjectType { get; set; }
		public string DiagramAssetType { get; set; }
		public string NextAssetConnectorLabel { get; set; }
		public decimal NextAssetStepNo { get; set; }
		public string NextAssetName { get; set; }
		public string AssetUID { get; set; }
		public long AssetID { get; set; }
		public string AssetURL { get; set; }
		public string NextAssetUID { get; set; }
		public long NextAssetID { get; set; }
		public string NextAssetURL { get; set; }
	}

	public class ProcessDiagramExportNodeTypeModel
	{
		public string AssetTypeUid { get; set; }
		public string AssetTypeName { get; set; }
		public string assets { get; set; }
	}

	public class ProcessDiagramExportRelatedAssetModel
	{
		public long DiagramAssetId { get; set; }
		public string DiagramAssetUid { get; set; }
		public decimal StepNo { get; set; }
		public string DiagramAssetName { get; set; }
		public string AssetUid { get; set; }
		public string AssetTypeUid { get; set; }
		public string AssetTypeName { get; set; }
		public string PredicateUid { get; set; }
	}

	public class ProcessDiagramExportModel
	{
		public List<ProcessDiagramExportNameValueModel> AssetProperties { get; set; }
		public List<ProcessDiagramExportNodeModel> Nodes { get; set; }
		public List<ProcessDiagramExportNodeTypeModel> NodeTypes { get; set; }
		public List<ProcessDiagramExportRelatedAssetModel> RelatedAssets { get; set; }
	}
}
