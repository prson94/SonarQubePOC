using d360.model.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using d360.core.resources;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.validators
{
    public class SurveyApiModelValidator : ISurveyApiModelValidator
    {
        IAssetRepository assetRepository;
        IResourceRepository resourceRepository;
        ISurveyRepository surveyRepository;
        public SurveyApiModelValidator(IAssetRepository assetRepository, IResourceRepository resourceRepository, ISurveyRepository surveyRepository)
        {
            this.assetRepository = assetRepository;
            this.resourceRepository = resourceRepository;
            this.surveyRepository = surveyRepository;
        }
       

        public bool IsValidResource(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            bool isValid = true;
            if (queryParams.ToList().Any(q => q.Key.ToLower() == "resourceuid"))
            {
                Guid resourceUID;
                var resourceString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "resourceuid").Value;
                if (Guid.TryParse(resourceString, out resourceUID))
                {
                    var resource = this.resourceRepository.GetResouceByUID(resourceUID);
                    if (resource == null)
                        isValid = false;
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
            if (queryParams.ToList().Any(q => q.Key.ToLower() == "assetuid"))
            {
                Guid assetUID;
                var assetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assetuid").Value;
                if (Guid.TryParse(assetUIDString, out assetUID))
                {
                    var asset = this.assetRepository.GetAssetByUID(assetUID);
                    if (asset == null)
                        isValid = false;
                }
                else
                {
                    isValid = false;
                }

            }
            return isValid;
        }

        public bool IsValidDate(IEnumerable<KeyValuePair<string, string>> queryParams,string parameterName)
        {
            bool isValid = true;
            if (queryParams.ToList().Any(q => q.Key.ToLower() == parameterName.ToLower()))
            {
                DateTime sdate;
                var sdateString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName.ToLower()).Value;
                if (!DateTime.TryParse(sdateString, out sdate))
                    isValid = false;
            }
            return isValid;
        }


        public bool IsValidSurveyType(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            bool isValid = true;
            if (queryParams.ToList().Any(q => q.Key.ToLower() == "surveytypeuid"))
            {
                Guid surveytypeUID;
                var surveytypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "surveytypeuid").Value;
                if (Guid.TryParse(surveytypeUIDString, out surveytypeUID))
                {
                    var surveyType = this.surveyRepository.GetSurveyTypeByUid(surveytypeUID);
                    if (surveyType == null)
                        isValid = false;
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
                queryParams.ToList().ForEach(x => {
                    switch (x.Key.ToLower())
                    {
                        case "surveytypeuid":
                            Guid actionTypeUid;

                            if ((Guid.TryParse(x.Value, out actionTypeUid)) && (actionTypeUid != Guid.Empty))
                            {
                                count++;
                            }
                            break;
                        case "resourceuid":
                            Guid assetTypeUid;
                            if ((Guid.TryParse(x.Value, out assetTypeUid)) && (assetTypeUid != Guid.Empty))
                            {
                                count++;
                            }
                            break;
                        case "assetuid":
                            Guid relationshipTypeUid;
                            if ((Guid.TryParse(x.Value, out relationshipTypeUid)) && (relationshipTypeUid != Guid.Empty))
                            {
                                count++;
                            }
                            break;
                    }
                });

            }

            return (count > 0);
        }

        public WorkHttpStatus ValidateGetSurveyTypesRequest(IEnumerable<KeyValuePair<string, string>> queryParams)
        {

            foreach (var param in queryParams)
            {
                switch (param.Key.ToLower())
                {
                    case "assettypeuid":
                        if (!Guid.TryParse(param.Value, out Guid _))
                            return new WorkHttpStatus(System.Net.HttpStatusCode.BadRequest,AssetTypeErrors.BadRequest,OthersError.InvalidAssetTypeUid);
                        break;
                    case "surveytypeuid":
                        if (!Guid.TryParse(param.Value, out Guid _))
                            return new WorkHttpStatus(System.Net.HttpStatusCode.BadRequest,AssetTypeErrors.BadRequest, OthersError.InvalidSurveyTypeUid);
                        break;
                    case "_pagesize":
                        if (!int.TryParse(param.Value, out int _))
                            return new WorkHttpStatus(System.Net.HttpStatusCode.BadRequest,AssetTypeErrors.BadRequest, OthersError.InvalidPageSize);
                        break;
                    case "_pagenum":
                        if (!int.TryParse(param.Value, out _))
                            return new WorkHttpStatus(System.Net.HttpStatusCode.BadRequest,AssetTypeErrors.BadRequest,OthersError.InvalidPageNum);
                        break;
                    case "_order":
                        switch (param.Value.ToLower())
                        {
                            case "name":
                            case "validfordays":
                            case "createdon":
                            case "updatedon":
                            case "numberofresponses":
                                break;
                            default:
                                return new WorkHttpStatus(System.Net.HttpStatusCode.BadRequest,AssetTypeErrors.BadRequest,OthersError.InvalidOrderSurvey);
                        }
                        break;
                }
            }

            return null;
        }

    }
}
