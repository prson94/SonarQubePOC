using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.validators
{
    public interface IWorkflowApiModelValidator
    {
       bool ValidateWorkflowGetTypeModel(IEnumerable<KeyValuePair<string, string>> queryParams);
        bool IsValidGuidCountForWorkflowGetVersionModel(IEnumerable<KeyValuePair<string, string>> queryParams);
        bool IsValidGuidForWorkflowGetVersionModel(IEnumerable<KeyValuePair<string, string>> queryParams);
    }
}
