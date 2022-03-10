using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;
using d360.core.entities.Process;

namespace d360.model.DataAccessLayer
{
    public interface IProcessRepository
    {
        ProcessDiagramModel GetAssetsProcessDiagram(Guid assetUid);
        
        Task<IEnumerable<dynamic>> GetAvailableDiagramNodesForAsset(Guid assetUid);
        
        List<ValidationError> UpdateProcessDiagram(ApiExecution execution, ProcessDiagramModel model, List<NodeData> toAdd, List<NodeData> toUpdate, List<NodeData> toDelete, long targetAssetId, bool isDiagramReplace, List<ProcessDiagramCopyRelationshipModel> copyRelationshipModel, List<ProcessDiagramCopyMapper> pdCopyMapper);
        
        Task<byte[]> GetDiagramExcel(Asset asset, byte[] image);
        
        IEnumerable<ProcessDiagramBadge> GetDiagramAssetBadges(Guid assetUid);
    }
}
