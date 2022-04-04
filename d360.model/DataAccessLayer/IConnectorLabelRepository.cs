using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;

namespace d360.model.DataAccessLayer
{
    public interface IConnectorLabelRepository
    {
        ConnectorLabelApiModel CreateConnectorLabel(ConnectorLabelPostModel model);
        
        bool DeleteConnectorLabels(List<ConnectorLabelApiDeleteModel> model);
        
        bool DoesLabelExists(Guid uid);
        
        bool DoesLabelExists(Guid existingUid, ConnectorLabelPostModel model);
        
        bool DoesLabelExists(string value);
        
        Task<ConnectorLabelApiModelWrapper> GetLabels(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        ConnectorLabelApiModel UpdateConnectorLabel(Guid uid, ConnectorLabelPostModel model, ConnectorLabel existingLabel);
        
        Task<dynamic> GetConnectorLabelsForExcel(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        IEnumerable<dynamic> GetConnectorLabelUsage(Guid labelUid, IEnumerable<KeyValuePair<string, string>> queryParams);
        
        (byte[], string) GetExcelFromConnectorLabelUsage(ConnectorLabel label, IEnumerable<dynamic> response);
        
        bool IsAuthorizedToEditConnectorLabel(Guid tagUid);
    }
}
