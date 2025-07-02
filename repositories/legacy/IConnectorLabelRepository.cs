using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;

namespace repositories
{
	public interface IConnectorLabelRepository
	{
		Task<ConnectorLabelApiModel> CreateConnectorLabel(ConnectorLabelPostModel model);

		Task<bool> DeleteConnectorLabels(List<ConnectorLabelApiDeleteModel> model);

		Task<bool> DoesLabelExists(Guid uid);

		Task<bool> DoesLabelExists(Guid existingUid, ConnectorLabelPostModel model);

		Task<bool> DoesLabelExists(string value);

		Task<ConnectorLabelApiModelWrapper> GetLabels(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<ConnectorLabelApiModel> UpdateConnectorLabel(Guid uid, ConnectorLabelPostModel model, ConnectorLabel existingLabel);

		Task<dynamic> GetConnectorLabelsForExcel(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<IEnumerable<dynamic>> GetConnectorLabelUsage(Guid labelUid, IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<(byte[], string)> GetExcelFromConnectorLabelUsage(ConnectorLabel label, IEnumerable<dynamic> response);

		Task<bool> IsAuthorizedToEditConnectorLabel(Guid tagUid);

		Task<dynamic> GetLabels(string q = null, bool isExact = false, bool getUseCount = false, Guid? exceptUid = null);

		Task<ConnectorLabel> GetLabel(Guid parentGuid);

		Task<ConnectorLabel> GetLabel(string labelName);
	}
}