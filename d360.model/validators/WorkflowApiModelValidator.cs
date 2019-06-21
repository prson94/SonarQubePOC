using d360.model.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.validators
{
    public class WorkflowApiModelValidator : IWorkflowApiModelValidator
    {
        IAssetRepository assetRepository;
        IIssueRepository issueRepository;
        IRelationshipRepository relationshipRepository;
        IWorkflowRepository workflowRepository;

        public WorkflowApiModelValidator(IAssetRepository assetRepository, IIssueRepository issueRepository, 
            IRelationshipRepository relationshipRepository,
            IWorkflowRepository workflowRepository)
        {
            this.assetRepository = assetRepository;
            this.issueRepository = issueRepository;
            this.relationshipRepository = relationshipRepository;
            this.workflowRepository = workflowRepository;
        }

    



    public bool IsValidGuidCountForWorkflowGetTypeModel(IEnumerable<KeyValuePair<string, string>> queryParams)
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

    public bool IsValidGuidForWorkflowGetTypeModel(IEnumerable<KeyValuePair<string, string>> queryParams)
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
                      
                }

            });
        }

        return isValidGuid;
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
                        case "workflowtypeuid":
                            Guid workflowTypeUid;
                            if ((Guid.TryParse(x.Value, out workflowTypeUid)) && (workflowTypeUid != Guid.Empty))
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

        public bool IsValidAssetType(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            bool isValid = true;
            if (queryParams.ToList().Any(q => q.Key.ToLower() == "assettypeuid"))
            {
                Guid assettypeUID;
                var assettypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value;
                if (Guid.TryParse(assettypeUIDString, out assettypeUID))
                {
                    var assetType = this.assetRepository.GetAssetTypeByUID(assettypeUID);
                    if (assetType == null)
                        isValid = false;
                }
                else
                {
                    isValid = false;
                }

            }
            return isValid;
        }

        public bool IsValidActionType(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            bool isValid = true;
            if (queryParams.ToList().Any(q => q.Key.ToLower() == "actiontypeuid"))
            {
                Guid actiontypeUID;
                var assettypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "actiontypeuid").Value;
                if (Guid.TryParse(assettypeUIDString, out actiontypeUID))
                {
                    var issueType = this.issueRepository.GetIssueTypeByUID(actiontypeUID);
                    if (issueType == null)
                        isValid = false;
                }
                else
                {
                    isValid = false;
                }

            }
            return isValid;
        }

        public bool IsValidRelationshipType(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            bool isValid = true;
            if (queryParams.ToList().Any(q => q.Key.ToLower() == "relationshiptypeuid"))
            {
                Guid relationshiptypeUID;
                var relationshiptypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "relationshiptypeuid").Value;
                if (Guid.TryParse(relationshiptypeUIDString, out relationshiptypeUID))
                {
                    var relationshiptype = this.relationshipRepository.GetRelationshipByUID(relationshiptypeUID);
                    if (relationshiptype == null)
                        isValid = false;
                }
                else
                {
                    isValid = false;
                }

            }
            return isValid;
        }

        public bool IsValidWorkflowType(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            bool isValid = true;
            if (queryParams.ToList().Any(q => q.Key.ToLower() == "workflowtypeuid"))
            {
                Guid workflowtypeUID;
                var workflowtypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "workflowtypeuid").Value;
                if (Guid.TryParse(workflowtypeUIDString, out workflowtypeUID))
                {
                    var workflowType = this.workflowRepository.GetWorkflowTypeByUID(workflowtypeUID);
                    if (workflowType == null)
                        isValid = false;
                }
                else
                {
                    isValid = false;
                }

            }
            return isValid;
        }

        public bool IsValidWorkflowVersion(Guid workflowVersionUID)
        {
            bool isValid = true;
            var workflowVersionType = this.workflowRepository.GetWorkflowVersionByUID(workflowVersionUID);
            if (workflowVersionType == null)
                isValid = false;
            return isValid;
        }

        public bool IsValidWorkflowInstance(Guid workflowInstanceUID)
        {
            bool isValid = true;
            var workflowInstance = this.workflowRepository.GetWorkflowItemByUID(workflowInstanceUID);
            if (workflowInstance == null)
                isValid = false;
            return isValid;
        }

        public bool IsValidOrderByFieldForWorkflowGetVersionModel(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            if (queryParams.Any(p => p.Key.Trim().ToLower() == "_order"))
            {
                var fieldName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_order").Value;
                string[] validFields = { "versionnumber", "state", "createdon", "updatedon" };
            
                return validFields.Contains(fieldName.Trim().ToLower());
            }
            return true;
        }
    }
}
