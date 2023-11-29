using d360.core.entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IWorkflow
	{
		Platform Platform { get; }

		Task<IssueType> CreateActionType();

		Task<IList<Issue>> ReadActions();

		Task ReadActionTypeDefinition();

		Task<IList<IssueType>> ReadActionTypes();

		Task RemoveActionType();

		Task UpdateActionType();
	}
}
