using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using d360.core.entities.SurveyModels;
using d360.core.resources;
using d360.model.DataAccessLayer;
using SmartFormat;

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

		public async Task<WorkHttpStatus> ValidateSurveyTypeCreateApiModel(SurveyTypeCreateApiModel surveyType)
		{
			if (string.IsNullOrEmpty(surveyType.Name))
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					SurveyTypeErrors.NameShouldBeNotEmpty);
			}

			var maxNameLength = 250;
			if (!(surveyType.Name.Length <= maxNameLength))
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					Smart.Format(SurveyTypeErrors.NameIsTooBig, new { maxNameLength }));
			}

			var minValidForDays = 0;
			if (!(surveyType.ValidForDays >= minValidForDays))
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					Smart.Format(SurveyTypeErrors.ValidForDaysIsTooSmall, new { minValidForDays }));
			}

			var maxValidForDays = 365;
			if (!(surveyType.ValidForDays <= maxValidForDays))
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					Smart.Format(SurveyTypeErrors.ValidForDaysIsTooBig, new { maxValidForDays }));
			}

			var assetType = this.assetRepository.GetAssetTypeByUID(surveyType.AssetTypeUid);
			if (assetType == null)
			{
				return new WorkHttpStatus(
					HttpStatusCode.NotFound,
					AssetTypeErrors.NotFound,
					AssetTypeErrors.NotFoundGeneric);
			}

			if (!await surveyRepository.IsUniqueSurveyTypeName(surveyType.Name, assetType.ID, surveyTypeUid: null))
			{
				return new WorkHttpStatus(
					HttpStatusCode.Conflict,
					AssetTypeErrors.DuplicateFound,
					SurveyTypeErrors.DuplicateName);
			}

			return null;
		}

		public async Task<WorkHttpStatus> ValidateSurveyTypeUpdateApiModel(
			Guid surveyTypeUid,
			SurveyTypeUpdateApiModel updateModel)
		{
			if (string.IsNullOrEmpty(updateModel.Name))
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					SurveyTypeErrors.NameShouldBeNotEmpty);
			}

			var maxNameLength = 250;
			if (!(updateModel.Name.Length <= maxNameLength))
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					Smart.Format(SurveyTypeErrors.NameIsTooBig, new { maxNameLength }));
			}

			var minValidForDays = 0;
			if (!(updateModel.ValidForDays >= minValidForDays))
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					Smart.Format(SurveyTypeErrors.ValidForDaysIsTooSmall, new { minValidForDays }));
			}

			var maxValidForDays = 365;
			if (!(updateModel.ValidForDays <= maxValidForDays))
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					Smart.Format(SurveyTypeErrors.ValidForDaysIsTooBig, new { maxValidForDays }));
			}

			var existingSurveyType = this.surveyRepository.GetSurveyTypeByUid(surveyTypeUid);
			if (existingSurveyType == null)
			{
				return new WorkHttpStatus(
					HttpStatusCode.NotFound,
					AssetTypeErrors.NotFound,
					SurveyTypeErrors.SurveyTypeNotFound);
			}

			if (!await surveyRepository.IsUniqueSurveyTypeName(updateModel.Name, existingSurveyType.AssetTypeID, surveyTypeUid))
			{
				return new WorkHttpStatus(
					HttpStatusCode.Conflict,
					AssetTypeErrors.DuplicateFound,
					SurveyTypeErrors.DuplicateNameWhenUpdating);
			}

			return null;
		}
		public async Task<WorkHttpStatus> ValidateSurveyTypeDelete(Guid surveyTypeUid)
		{
			var existingSurveyType = this.surveyRepository.GetSurveyTypeByUid(surveyTypeUid);
			if (existingSurveyType == null)
			{
				return new WorkHttpStatus(
					HttpStatusCode.NotFound,
					AssetTypeErrors.NotFound,
					SurveyTypeErrors.SurveyTypeNotFound);
			}

			return null;
		}

		public async Task<WorkHttpStatus> ValidateQuestionTypeCreate(Guid surveyTypeUid, QuestionTypeUpsertModel question)
		{
			return await ValidateQuestionTypeUpsertModel(surveyTypeUid, question, questionTypeUid: null);
		}

		public async Task<WorkHttpStatus> ValidateQuestionTypeUpdate(
			Guid surveyTypeUid, 
			Guid questionTypeUid, 
			QuestionTypeUpsertModel question)
		{
			var commonValidation = await ValidateQuestionTypeUpsertModel(surveyTypeUid, question, questionTypeUid);
			if (commonValidation != null)
			{
				return commonValidation;
			}

			var existingQuestionType = this.surveyRepository.GetSurveyQuestionTypeByUid(questionTypeUid);
			if (existingQuestionType == null || existingQuestionType.SurveyType.Uid != surveyTypeUid)
			{
				return new WorkHttpStatus(
					HttpStatusCode.NotFound,
					AssetTypeErrors.NotFound,
					SurveyTypeErrors.QuestionTypeNotFound); 
			}

			if (!AreSameExceptDescription(existingQuestionType, question))
			{
				var hasAnswers = await this.surveyRepository.QuestionHasAnswers(questionTypeUid);
				if (hasAnswers)
				{
					return new WorkHttpStatus(
						HttpStatusCode.BadRequest,
						AssetTypeErrors.BadRequest,
						SurveyTypeErrors.SubmittedQuestionCanChangeOnlyDescription);
				}
			}

			bool AreSameExceptDescription(core.entities.QuestionType existing, QuestionTypeUpsertModel update)
			{
				if (existing.Name != update.Name)
				{
					return false;
				}

				if (existing.DisplayStyle != update.DisplayStyle)
				{
					return false;
				}

				if (existing.QuestionTypeOptions.Count != update.Options.Count)
				{
					return false;
				}

				var options = Enumerable.Zip(
					existing.QuestionTypeOptions,
					update.Options, 
					(existingOpt, updateOpt) => (existingOpt, updateOpt));

				foreach (var (existingOpt, updateOpt) in options)
				{
					if (existingOpt.Name != updateOpt.Name)
					{
						return false;
					}

					if (existingOpt.Value != updateOpt.Value)
					{
						return false;
					}
				}

				return true;
			}

			return null;
		}

		private async Task<WorkHttpStatus> ValidateQuestionTypeUpsertModel(
			Guid surveyTypeUid,
			QuestionTypeUpsertModel question,
			Guid? questionTypeUid)
		{
			if (string.IsNullOrEmpty(question.Name))
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					SurveyTypeErrors.NameShouldBeNotEmpty);
			}

			var maxNameLength = 500;
			var isValidNameLength = (question.Name.Length <= maxNameLength);
			if (!isValidNameLength)
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					Smart.Format(SurveyTypeErrors.NameIsTooBig, new { maxNameLength }));
			}

			var maxDescriptionLength = 2000;
			var isValidDescriptionLength = question.Description == null
				|| question.Description.Length <= maxDescriptionLength;

			if (!isValidDescriptionLength)
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					Smart.Format(SurveyTypeErrors.DescriptionTooBig, new { maxDescriptionLength }));
			}

			if (!Enum.IsDefined(typeof(core.QuestionDisplayStyle), question.DisplayStyle))
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					SurveyTypeErrors.InvalidDisplayStyle);
			}

			if (question.Options == null || question.Options.Count == 0)
			{
				return new WorkHttpStatus(
					HttpStatusCode.BadRequest,
					AssetTypeErrors.BadRequest,
					SurveyTypeErrors.MissingQuestionOptions);
			}

			foreach (var (option, index) in question.Options.Select((opt, index) => (opt, index)))
			{
				if (string.IsNullOrEmpty(option.Name))
				{
					return new WorkHttpStatus(
						HttpStatusCode.BadRequest,
						AssetTypeErrors.BadRequest,
						Smart.Format(SurveyTypeErrors.MissingQuestionOptionName, new { index }));
				}
			}

			var nonUniqueNames = question.Options
				.GroupBy(opt => opt.Name)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToHashSet();

			foreach (var (option, index) in question.Options.Select((opt, index) => (opt, index)))
			{
				if (nonUniqueNames.Contains(option.Name))
				{
					return new WorkHttpStatus(
						HttpStatusCode.BadRequest,
						AssetTypeErrors.BadRequest,
						Smart.Format(SurveyTypeErrors.DuplicateQuestionOptionName, new { index }));
				}
			}

			var nonUniqueValues = question.Options
				.GroupBy(opt => opt.Value)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToHashSet();

			foreach (var (option, index) in question.Options.Select((opt, index) => (opt, index)))
			{
				if (nonUniqueValues.Contains(option.Value))
				{
					return new WorkHttpStatus(
						HttpStatusCode.BadRequest,
						AssetTypeErrors.BadRequest,
						Smart.Format(SurveyTypeErrors.DuplicateQuestionOptionValue, new { index }));
				}
			}

			var existingSurveyType = this.surveyRepository.GetSurveyTypeByUid(surveyTypeUid);
			if (existingSurveyType == null)
			{
				return new WorkHttpStatus(
					HttpStatusCode.NotFound,
					AssetTypeErrors.NotFound,
					SurveyTypeErrors.SurveyTypeNotFound);
			}

			if (!await surveyRepository.IsUniqueQuestionTypeName(question.Name, existingSurveyType.ID, questionTypeUid))
			{
				return new WorkHttpStatus(
					HttpStatusCode.Conflict,
					AssetTypeErrors.DuplicateFound,
					SurveyTypeErrors.DuplicateQuestionNameWhenUpdating);
			}

			return null;
		}
	}
}
