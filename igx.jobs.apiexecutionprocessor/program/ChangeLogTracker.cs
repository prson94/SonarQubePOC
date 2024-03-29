using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using d360.core;
using d360.core.enums;
using d360.model;
using System.Data.SqlClient;
using Dapper;

namespace igx.jobs.apiexecutionprocessor.helpers
{
	public enum ChangeAction
	{
		Created, Updated, Removed
	}

	public class FieldValueState
	{
		public string FieldName { get; set; }
		public string Value { get; set; }
	}

	public class ChangeLogTracker<T> where T : class
	{
		private readonly SqlConnection _connection;
		private readonly T lastState;

		private T originalState;
		private ChangeAction action;

		public bool ShouldBeLogged { get; set; }

		private Audit _audit = new Audit();
		public ChangeLogTracker(T lastState, SqlConnection connection)
		{
			_connection = connection;
			this.lastState = lastState;
		}

		public void ParseAndSaveAuditRecord()
		{
			SetInitialState();
			ParseAuditRecord();
			var a = this._audit;
		}

		private void SetInitialState()
		{
			if (lastState is AssetType)
			{
				var obj = lastState as AssetType;
				var originalValues = _connection.Query<FieldValueState>(_lastVersionFieldLogsSQL, new { obj.Object, obj.ObjectID });
				originalState = (T)Activator.CreateInstance(typeof(T));

				var properties = originalState.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(TrackInChangeLog), false));
				foreach (var prop in properties)
				{
					var value = originalValues.FirstOrDefault(x => x.FieldName == prop.Name && !string.IsNullOrEmpty(x.Value));
					prop.SetValue(originalState, value);
				}
			}
		}

		private void ParseAuditRecord()
		{
			if (this.lastState is AssetType)
			{
				AssetType at = this.lastState as AssetType;

				DateTime updatedOn = at.UpdatedOn ?? at.CreatedOn ?? DateTime.UtcNow;
				int updatedBy = at.UpdatedBy ?? at.CreatedBy ?? 0;

				_audit = new Audit
				{
					Object = at.Object,
					ObjectID = at.ObjectID,
					ActionObject = at.Object,
					ActionObjectID = at.ObjectID,
					Action = action.ToString(),
					ObjectName = at.Name,
					ActionObjectName = at.Name,
					ActionObjectTypeName = at.Class.GetDisplayName(),
					ActionDescription = $"This asset type has been {action.ToString()}",
					Date = updatedOn,
					ResourceID = updatedBy,
					AuditFields = new List<AuditField>()
				};
			}

			if (lastState is FieldType)
			{
				FieldType ft = lastState as FieldType;

				var actionObject = "";
				var actionObjectId = 0;

				if (ft.AssetTypeID.HasValue)
				{
					var at = _companyContext.AssetTypes.Select(x => new { x.ObjectID, x.Object, x.ID }).FirstOrDefault(x => x.ID == ft.AssetTypeID);
					actionObject = at.Object;
					actionObjectId = at.ObjectID;
				}
				else if (ft.IssueTypeID.HasValue)
				{
					actionObject = "IssueType";
					actionObjectId = ft.IssueTypeID.Value;

				}
				else if (ft.IntersectTypeID.HasValue)
				{
					actionObject = "IntersectType";
					actionObjectId = ft.IntersectTypeID.Value;
				}

				_audit = new Audit
				{
					Object = "FieldType",
					ObjectID = ft.ID,
					ActionObject = actionObject,
					ActionObjectID = actionObjectId,
					Action = action.ToString(),
					ObjectName = ft.FriendlyName,
					ActionObjectName = ft.FriendlyName,
					ActionObjectTypeName = "Field Type",
					ActionDescription = $"This field type has been {action.ToString().ToLowerInvariant()}",
					Date = DateTime.UtcNow,
					ResourceID = ft.UpdatedBy,
					AuditFields = new List<AuditField>()
				};
			}


			var currentVersion = _companyContext.Audits.Where(x => x.ActionObject == _audit.ActionObject && x.ActionObjectID == _audit.ActionObjectID).OrderByDescending(x => x.Version).Select(x => x.Version).FirstOrDefault();
			_audit.Version = currentVersion + 1;

			if (action != ChangeAction.Removed)
			{
				HandleFieldUpdates();
			}
			ShouldBeLogged = action == ChangeAction.Removed || _audit.AuditFields.Count > 0;
		}

		private void HandleFieldUpdates()
		{
			var properties = originalState.GetType().GetProperties()
									.Where(prop => prop.IsDefined(typeof(TrackInChangeLog), false));

			foreach (var prop in properties)
			{
				var _val = prop.GetValue(this.lastState, null);
				string oldValue = null;
				string newValue = _val != null ? _val.ToString() : null;

				if (action == ChangeAction.Updated)
				{
					var oldValAsObj = prop.GetValue(this.originalState, null);
					oldValue = oldValAsObj != null ? oldValAsObj.ToString() : null;
				}

				if ((oldValue ?? "") != (newValue ?? ""))
				{
					_audit.AuditFields.Add(
						new AuditField
						{
							FieldName = prop.Name,
							FieldTypeID = 0,
							PreviousValue = oldValue,
							Value = newValue
						});
				}
			}
		}

		private readonly string _lastVersionFieldLogsSQL = $@"
				drop table if exists #changelogs

				select gfa.FieldName, gfa.Value, ga.Version
				into #changelogs
				from reporting.Global_Audit GA
				inner join reporting.Global_FieldAudit gfa on gfa.AuditID = ga.ID
				where ActionObject = @object and ActionObjectID = @objectId and Object = @object and ObjectID = @objectId

				select * from #changelogs logs
				outer apply (select max(version) from #changelogs where fieldname = logs.FieldName)L(MaxVersion)
				where logs.Version = l.MaxVersion;";

	}


}
