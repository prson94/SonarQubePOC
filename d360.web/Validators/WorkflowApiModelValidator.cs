using System;
using System.Collections.Generic;
using System.Linq;
using repositories;
using System.Threading.Tasks;

namespace d360.web.validators
{
	public class WorkflowApiModelValidator : IWorkflowApiModelValidator
	{
		private readonly IAssetRepository AssetRepository;
		private readonly IWorkflow IssueRepository;
		private readonly IRelationshipRepository RelationshipRepository;
		private readonly IWorkflowRepository WorkflowRepository;

		public WorkflowApiModelValidator(IAssetRepository assetRepository, IWorkflow issueRepository,
			IRelationshipRepository relationshipRepository,
			IWorkflowRepository workflowRepository)
		{
			AssetRepository = assetRepository;
			IssueRepository = issueRepository;
			RelationshipRepository = relationshipRepository;
			WorkflowRepository = workflowRepository;
		}

		public bool IsValidGuidCountForWorkflowGetTypeModel(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			int count = 0;

			if (queryParams != null)
			{
				queryParams.ToList().ForEach(x =>
				{
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
				queryParams.ToList().ForEach(x =>
				{
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
				string assettypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value;
				if (Guid.TryParse(assettypeUIDString, out Guid assettypeUID))
				{
					core.entities.AssetType assetType = AssetRepository.GetAssetTypeByUID(assettypeUID);
					if (assetType == null)
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

		public bool IsValidActionType(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			bool isValid = true;

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "actiontypeuid"))
			{
				string assettypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "actiontypeuid").Value;
				if (Guid.TryParse(assettypeUIDString, out Guid actiontypeUID))
				{
					core.entities.IssueType issueType = Task.Run(() => IssueRepository.GetIssueTypeByUIDAsync(actiontypeUID)).GetAwaiter().GetResult();

					if (issueType == null)
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

		public bool IsValidRelationshipType(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			bool isValid = true;

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "relationshiptypeuid"))
			{
				string relationshiptypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "relationshiptypeuid").Value;
				if (Guid.TryParse(relationshiptypeUIDString, out Guid relationshiptypeUID))
				{
					core.entities.IntersectType relationshiptype = RelationshipRepository.GetRelationshipTypeByUID(relationshiptypeUID);
					if (relationshiptype == null)
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

		public bool IsValidWorkflowType(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			bool isValid = true;

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "workflowtypeuid"))
			{
				string workflowtypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "workflowtypeuid").Value;
				if (Guid.TryParse(workflowtypeUIDString, out Guid workflowtypeUID))
				{
					core.entities.Workflow.Type workflowType = WorkflowRepository.GetWorkflowTypeByUID(workflowtypeUID);
					if (workflowType == null)
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

		public bool IsValidWorkflowVersion(Guid workflowVersionUID)
		{
			core.entities.Workflow.WorkflowVersion workflowVersionType = WorkflowRepository.GetWorkflowVersionByUID(workflowVersionUID);
			return workflowVersionType != null;
		}

		public bool IsValidWorkflowInstance(Guid workflowInstanceUID)
		{
			core.entities.Workflow.WorkflowItem workflowInstance = WorkflowRepository.GetWorkflowItemByUID(workflowInstanceUID);
			return workflowInstance != null;
		}

		public bool IsValidOrderByFieldForWorkflowGetVersionModel(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			if (queryParams.Any(p => p.Key.Trim().ToLower() == "_order"))
			{
				string fieldName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_order").Value;
				string[] validFields = { "versionnumber", "state", "createdon", "updatedon" };

				return validFields.Contains(fieldName.Trim().ToLower());
			}

			return true;
		}

		public bool IsValidGuidCountForGetWorkflowModel(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			int count = 0;

			if (queryParams != null)
			{
				queryParams.ToList().ForEach(x =>
				{
					switch (x.Key.ToLower())
					{
						case "actionuid":
							Guid actionTypeUid;

							if ((Guid.TryParse(x.Value, out actionTypeUid)) && (actionTypeUid != Guid.Empty))
							{
								count++;
							}
							break;

						case "assetuid":
							Guid assetTypeUid;
							if ((Guid.TryParse(x.Value, out assetTypeUid)) && (assetTypeUid != Guid.Empty))
							{
								count++;
							}
							break;

						case "relationshipuid":
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

		public bool IsValidAsset(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			bool isValid = true;

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "assetuid"))
			{
				string assetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assetuid").Value;
				if (Guid.TryParse(assetUIDString, out Guid assetUID))
				{
					core.entities.Asset asset = AssetRepository.GetAssetByUID(assetUID);

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

		public bool IsValidAction(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			bool isValid = true;

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "actionuid"))
			{
				string assetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "actionuid").Value;

				if (Guid.TryParse(assetUIDString, out Guid actionUID))
				{
					core.entities.Issue issueType = Task.Run(() => IssueRepository.GetIssueByUIDAsync(actionUID)).GetAwaiter().GetResult();

					if (issueType == null)
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

		public bool IsValidRelationship(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			bool isValid = true;

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "relationshipuid"))
			{
				string relationshipUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "relationshipuid").Value;

				if (Guid.TryParse(relationshipUIDString, out Guid relationshipUID))
				{
					core.entities.Intersect relationship = RelationshipRepository.GetRelationshipByUID(relationshipUID);

					if (relationship == null)
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

		public bool IsValidGuidForGetWorkflowModel(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			bool isValidGuid = true;

			if (queryParams != null)
			{
				queryParams.ToList().ForEach(x =>
				{
					Guid uid;
					switch (x.Key.ToLower())
					{
						case "actionuid":
							if (!Guid.TryParse(x.Value, out uid))
							{
								isValidGuid = false;
							}
							break;

						case "assetuid":
							if (!Guid.TryParse(x.Value, out uid))
							{
								isValidGuid = false;
							}
							break;

						case "relationshipuid":
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

						case "versionuid":
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

		public bool IsValidOrderByFieldForGetWorkflowModel(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			if (queryParams.Any(p => p.Key.Trim().ToLower() == "_order"))
			{
				string fieldName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_order").Value;
				string[] validFields = { "startedon", "completedon" };

				return validFields.Contains(fieldName.Trim().ToLower());
			}

			return true;
		}

		public bool IsValidWorkflowVersion(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			bool isValid = true;

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "versionuid"))
			{
				string workflowVersionUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "versionuid").Value;

				if (Guid.TryParse(workflowVersionUIDString, out Guid workflowVersionUID))
				{
					core.entities.Workflow.WorkflowVersion workflowVersion = WorkflowRepository.GetWorkflowVersionByUID(workflowVersionUID);

					if (workflowVersion == null)
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

		public bool IsValidDirectionForWorkflowGetModel(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			string[] allowedValues = new string[] { "asc", "desc" };
			KeyValuePair<string, string> directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction");

			if (directionFilter.Key == null)
			{
				return true;
			}

			if (!allowedValues.Contains(directionFilter.Value.Trim().ToLower()))
			{
				return false;
			}

			return true;
		}
	}
}