using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.validators
{
    public class WorkflowApiModelValidator : IWorkflowApiModelValidator
    {
      
        public WorkflowApiModelValidator()
        {
            
        }

        public bool ValidateWorkflowGetTypeModel(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            int count = 0;
            if (queryParams.ToList().Any(q => q.Key.ToLower() == "actiontypeuid"))
            {
                Guid actionTypeUid;
                var actionTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "actiontypeuid").Value;
                if ((Guid.TryParse(actionTypeUidString, out actionTypeUid)) && (actionTypeUid != Guid.Empty))
                {
                    count++;
                }
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "assettypeuid"))
            {
                Guid assetTypeUid;
                var assettypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value;
                if ((Guid.TryParse(assettypeUidString, out assetTypeUid)) && (assetTypeUid != Guid.Empty))
                {
                    count++;
                }
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "relationshiptypeuid"))
            {
                Guid relationshipTypeUid;
                var relationshipTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "relationshiptypeuid").Value;
                if ((Guid.TryParse(relationshipTypeUidString, out relationshipTypeUid)) && (relationshipTypeUid != Guid.Empty))
                {
                    count++;
                }
            }

            return !(count >1);
        }

        public bool IsValidGuidCountForWorkflowGetVersionModel(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            int count = 0;
            if (queryParams != null)
            {
                queryParams.ToList().ForEach(x => {


                    switch (x.Key.ToLower())
                    {
                        case "actiontypeuid":
                            Guid actionTypeUid;

                            if ((Guid.TryParse(x.Value, out actionTypeUid)) && (actionTypeUid != Guid.Empty))
                            {
                                count++;
                            }
                            break;
                        case "assettypeuid":
                            Guid assetTypeUid;
                            if ((Guid.TryParse(x.Value, out assetTypeUid)) && (assetTypeUid != Guid.Empty))
                            {
                                count++;
                            }
                            break;
                        case "relationshiptypeuid":
                            Guid relationshipTypeUid;
                            if ((Guid.TryParse(x.Value, out relationshipTypeUid)) && (relationshipTypeUid != Guid.Empty))
                            {
                                count++;
                            }
                            break;
                    }
                });

            }

            return !(count > 1);

        }

        public bool IsValidGuidForWorkflowGetVersionModel(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            bool isValidGuid = true;
            if (queryParams != null)
            {
                queryParams.ToList().ForEach(x =>
                {

                    Guid uid;
                    switch (x.Key.ToLower())
                    {
                        case "actiontypeuid":
                            if (!Guid.TryParse(x.Value, out uid))
                            {
                                isValidGuid = false;
                            }
                            break;
                        case "assettypeuid":
                            if (!Guid.TryParse(x.Value, out uid))
                            {
                                isValidGuid = false;
                            }
                            break;
                        case "relationshiptypeuid":
                            if (!Guid.TryParse(x.Value, out uid))
                            {
                                isValidGuid = false;
                            }
                            break;
                        case "workflowtypeuid":
                            if (!Guid.TryParse(x.Value, out uid))
                            {
                                isValidGuid = false;
                            }
                            break;

                    }

                });
            }

            return isValidGuid;
        }
    }
}
