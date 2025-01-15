using d360.core.entities;
using d360.core.entities.Process;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IProcessRepository
	{
		Task<ProcessDiagramModel> GetAssetsProcessDiagram(Guid assetUid);

		Task<IEnumerable<dynamic>> GetAvailableDiagramNodesForAsset(Guid assetUid);

		Task<List<ValidationError>> UpdateProcessDiagram(ApiExecution execution, ProcessDiagramModel model, List<NodeData> toAdd, List<NodeData> toUpdate, List<NodeData> toDelete, long targetAssetId, bool isDiagramReplace, List<ProcessDiagramCopyRelationshipModel> copyRelationshipModel, List<ProcessDiagramCopyMapper> pdCopyMapper);

		Task<byte[]> GetDiagramExcel(Asset asset, byte[] image);

		Task<IEnumerable<ProcessDiagramBadge>> GetDiagramAssetBadges(Guid assetUid);
	}
}