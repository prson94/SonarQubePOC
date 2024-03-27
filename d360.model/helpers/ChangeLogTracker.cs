using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using d360.core;
using d360.core.enums;

namespace d360.model.helpers
{
	public class EFEntryState
	{ 
		public bool ShouldBeLogged { get; set; }
		private ObjectStateEntry _entry;
		private Audit _audit = new Audit();
		private readonly CompanyContext _companyContext;

		public EFEntryState(ObjectStateEntry entry, CompanyContext ctx)
		{
			this._entry = entry;
			_companyContext = ctx;

			ParseAuditRecord();
		}

		public Audit Audit { get { return _audit; } }

		private void ParseAuditRecord()
		{
			string action = "";
			switch (_entry.State)
			{
				case EntityState.Added:
					action = "Created";
					break;
				case EntityState.Modified:
					action = "Updated";
					break;
				case EntityState.Deleted:
					action = "Removed";
					break;
				default:
					action = "";
					break;
			}

			if (_entry.Entity is AssetType)
			{
				AssetType at = _entry.Entity as AssetType;

				DateTime updatedOn = at.UpdatedOn ?? at.CreatedOn ?? DateTime.UtcNow;
				int updatedBy = at.UpdatedBy ?? at.CreatedBy ?? 0;

				_audit = new Audit
				{
					Object = at.Object,
					ObjectID = at.ObjectID,
					ActionObject = at.Object,
					ActionObjectID = at.ObjectID,
					Action = action,
					ObjectName = at.Name,
					ActionObjectName = at.Name,
					ActionObjectTypeName = at.Class.GetDisplayName(),
					ActionDescription = $"This asset type has been {action.ToLowerInvariant()}",
					Date = updatedOn,
					ResourceID = updatedBy,
					AuditFields = new List<AuditField>()
				};
			}

			if (_entry.Entity is FieldType)
			{
				FieldType ft = _entry.Entity as FieldType;

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
					Action = action,
					ObjectName = ft.FriendlyName,
					ActionObjectName = ft.FriendlyName,
					ActionObjectTypeName = "Field Type",
					ActionDescription = $"This field type has been {action.ToLowerInvariant()}",
					Date = DateTime.UtcNow,
					ResourceID = ft.UpdatedBy,
					AuditFields = new List<AuditField>()
				};
			}


			var currentVersion = _companyContext.Audits.Where(x => x.ActionObject == _audit.ActionObject && x.ActionObjectID == _audit.ActionObjectID).OrderByDescending(x => x.Version).Select(x => x.Version).FirstOrDefault();
			_audit.Version = currentVersion + 1;

			if (_entry.State != EntityState.Deleted)
			{
				HandleFieldUpdates(_entry, _entry.Entity);
			}
			ShouldBeLogged = _entry.State == EntityState.Deleted || _audit.AuditFields.Count > 0;
		}

		private void HandleFieldUpdates<T>(ObjectStateEntry entry, T o)
		{
			var properties = o.GetType().GetProperties()
									.Where(prop => prop.IsDefined(typeof(TrackInChangeLog), false));

			foreach (var prop in properties)
			{
				var _val = prop.GetValue(o, null);
				string oldValue = null;
				string newValue = _val != null ? _val.ToString() : null;

				if (entry.State == EntityState.Modified)
				{
					var oldValAsObj = entry.OriginalValues[prop.Name];
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

		public void UpdateIds()
		{
			if (_entry.State != EntityState.Detached)
			{
				if (_entry.Entity is AssetType)
				{
					AssetType o = _entry.Entity as AssetType;

					_audit.ObjectID = o.ObjectID;
					_audit.ActionObjectID = o.ObjectID;
				}

				if (_entry.Entity is FieldType)
				{
					FieldType ft = _entry.Entity as FieldType;
					_audit.ObjectID = ft.ID;
				}
			}
		}
	}

	public class EFChangeTracker
	{
		private readonly List<EFEntryState> _trackedChanges = new List<EFEntryState>();
		private readonly CompanyContext _companyContext;
		public EFChangeTracker(CompanyContext ctx)
		{
			this._companyContext = ctx;
		}
		public void Add(ObjectStateEntry entry)
		{
			_trackedChanges.Add(new EFEntryState(entry, _companyContext));
		}

		public void SaveChangeLogs()
		{
			List<Audit> audits = new List<Audit>();

			_trackedChanges.ForEach(x => x.UpdateIds());
			_trackedChanges.Where(x => x.ShouldBeLogged).ToList().ForEach(x => audits.Add(x.Audit));

			if (audits.Count > 0)
			{
				_companyContext.Audits.AddRange(audits);
				_companyContext.SaveChanges();
			}
		}
	}
}
