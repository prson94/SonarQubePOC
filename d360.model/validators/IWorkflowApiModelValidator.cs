using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.validators
{
    public interface IWorkflowApiModelValidator
    {
        bool IsValidGuidCountForWorkflowGetTypeModel(IEnumerable<KeyValuePair<string, string>> queryParams);
        bool IsValidGuidForWorkflowGetTypeModel(IEnumerable<KeyValuePair<string, string>> queryParams);
    }
}
