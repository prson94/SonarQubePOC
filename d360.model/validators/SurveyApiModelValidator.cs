using System;
using System.Collections.Generic;
using System.Linq;

using d360.core.resources;
using d360.model.DataAccessLayer;

namespace d360.model.validators
{
    public class SurveyApiModelValidator : ISurveyApiModelValidator
    {
        private readonly IAssetRepository assetRepository;
        private readonly IResourceRepository resourceRepository;
        private readonly ISurveyRepository surveyRepository;

        public SurveyApiModelValidator(IAssetRepository assetRepository, IResourceRepository resourceRepository, ISurveyRepository surveyRepository)
        {
            this.assetRepository = assetRepository;
            this.resourceRepository = resourceRepository;
            this.surveyRepository = surveyRepository;
        }

        public bool IsValidResource(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            bool isValid = true;

            if (queryParams.ToList().Any(q => q.Key.ToLowerInvariant() == "resourceuid"))
            {
                string resourceString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLowerInvariant() == "resourceuid").Value;
               
                if (Guid.TryParse(resourceString, out Guid resourceUID))
                {
                    core.entities.GlobalReportingResource resource = resourceRepository.GetResouceByUID(resourceUID);
                    
                    if (resource == null)
                    {
                        isValid = false;
                    }
                }
                else
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        public bool IsValidAsset(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            bool isValid = true;

            if (queryParams.ToList().Any(q => q.Key.ToLowerInvariant() == "assetuid"))
            {
                string assetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLowerInvariant() == "assetuid").Value;
                
                if (Guid.TryParse(assetUIDString, out Guid assetUID))
                {
                    core.entities.Asset asset = assetRepository.GetAssetByUID(assetUID);
                    
                    if (asset == null)
                    {
                        isValid = false;
                    }
                }
                else
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        public bool IsValidDate(IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName)
        {
            bool isValid = true;

            if (queryParams.ToList().Any(q => q.Key.ToLowerInvariant() == parameterName.ToLowerInvariant()))
            {
                string sdateString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLowerInvariant() == parameterName.ToLowerInvariant()).Value;
                
                if (!DateTime.TryParse(sdateString, out DateTime sdate))
                {
                    isValid = false;
                }
            }

            return isValid;
        }


        public bool IsValidSurveyType(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            bool isValid = true;

            if (queryParams.ToList().Any(q => q.Key.ToLowerInvariant() == "surveytypeuid"))
            {
                string surveytypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLowerInvariant() == "surveytypeuid").Value;
                
                if (Guid.TryParse(surveytypeUIDString, out Guid surveytypeUID))
                {
                    core.entities.SurveyType surveyType = surveyRepository.GetSurveyTypeByUid(surveytypeUID);
                    
                    if (surveyType == null)
                    {
                        isValid = false;
                    }
                }
                else
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        public bool IsRequiredGuidExistForDeleteSurveyResult(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            int count = 0;

            if (queryParams != null)
            {
                queryParams.ToList().ForEach(x =>
                {
                    switch (x.Key.ToLowerInvariant())
                    {
                        case "surveytypeuid":
                            Guid actionTypeUid;

                            if (Guid.TryParse(x.Value, out actionTypeUid) && (actionTypeUid != Guid.Empty))
                            {
                                count++;
                            }
                            break;
                        case "resourceuid":
                            Guid assetTypeUid;
                            if (Guid.TryParse(x.Value, out assetTypeUid) && (assetTypeUid != Guid.Empty))
                            {
                                count++;
                            }
                            break;
                        case "assetuid":
                            Guid relationshipTypeUid;
                            if (Guid.TryParse(x.Value, out relationshipTypeUid) && (relationshipTypeUid != Guid.Empty))
                            {
                                count++;
                            }
                            break;
                        default:
                            //Nothing to do here.
                            break;
                    }
                });
            }

            return count > 0;
        }

        public WorkHttpStatus ValidateGetSurveyTypesRequest(IEnumerable<KeyValuePair<string, string>> queryParams)
        {

            foreach (KeyValuePair<string, string> param in queryParams)
            {
                switch (param.Key.ToLowerInvariant())
                {
                    case "assettypeuid":
                        if (!Guid.TryParse(param.Value, out Guid _))
                        {
                            return new WorkHttpStatus(System.Net.HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, OthersError.InvalidAssetTypeUid);
                        }
                        break;
                    case "surveytypeuid":
                        if (!Guid.TryParse(param.Value, out Guid _))
                        {
                            return new WorkHttpStatus(System.Net.HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, OthersError.InvalidSurveyTypeUid);
                        }
                        break;
                    case "_pagesize":
                        if (!int.TryParse(param.Value, out int _))
                        {
                            return new WorkHttpStatus(System.Net.HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, OthersError.InvalidPageSize);
                        }
                        break;
                    case "_pagenum":
                        if (!int.TryParse(param.Value, out _))
                        {
                            return new WorkHttpStatus(System.Net.HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, OthersError.InvalidPageNum);
                        }
                        break;
                    case "_order":
                        switch (param.Value.ToLowerInvariant())
                        {
                            case "name":
                            case "validfordays":
                            case "createdon":
                            case "updatedon":
                            case "numberofresponses":
                                break;
                            default:
                                return new WorkHttpStatus(System.Net.HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, OthersError.InvalidOrderSurvey);
                        }
                        break;
                    default:
                        //Nothing to do here.
                        break;
                }
            }

            return null;
        }
    }
}
