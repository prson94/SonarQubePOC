using d360.core.entities;
using d360.core.entities.Process;
using System;
using System.Collections.Generic;

namespace d360.model.DataAccessLayer
{
    public interface IProcessRepository
    {
        ProcessDiagramModel GetAssetsProcessDiagram(Guid assetUid);
        System.Threading.Tasks.Task<IEnumerable<dynamic>> GetAvailableDiagramNodesForAsset(Guid assetUid);
        List<ValidationError> UpdateProcessDiagram(ApiExecution execution, ProcessDiagramModel model, List<NodeData> toAdd, List<NodeData> toUpdate, List<NodeData> toDelete, long targetAssetId);
    }
}