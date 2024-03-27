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
		public ObjectStateEntry entry;
		public Audit Audit = new Audit();
		private CompanyContext CompanyContext;
		public EFEntryState(ObjectStateEntry entry, CompanyContext ctx)
		{
			this.entry = entry;
			CompanyContext = ctx;

			ParseAuditRecord();
		}

		private void ParseAuditRecord()
		{
			string action = "";
			switch (entry.State)
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
			}

			if (entry.Entity is AssetType)
			{
				AssetType at = entry.Entity as AssetType;

				DateTime updatedOn = at.UpdatedOn ?? at.CreatedOn ?? DateTime.UtcNow;
				int updatedBy = at.UpdatedBy ?? at.CreatedBy ?? 0;

				Audit = new Audit
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

			if (entry.Entity is FieldType)
			{
				FieldType ft = entry.Entity as FieldType;

				var actionObject = "";
				var actionObjectId = 0;
				var actionObjectName = "";

				if (ft.AssetTypeID.HasValue)
				{
					var at = CompanyContext.AssetTypes.Select(x => new { x.ObjectID, x.Object, x.Name, x.ID }).FirstOrDefault(x => x.ID == ft.AssetTypeID);
					actionObject = at.Object;
					actionObjectId = at.ObjectID;
					actionObjectName = at.Name;
				}
				else if (ft.IssueTypeID.HasValue)
				{
					var it = CompanyContext.IssueTypes.Select(x => new { x.Name, x.ID }).FirstOrDefault(x => x.ID == ft.AssetTypeID);
					actionObject = "IssueType";
					actionObjectId = ft.IssueTypeID.Value;
					actionObjectName = it.Name;

				}
				else if (ft.IntersectTypeID.HasValue)
				{
					actionObject = "IntersectType";
					actionObjectId = ft.IntersectTypeID.Value;
				}

				Audit = new Audit
				{
					Object = "FieldType",
					ObjectID = ft.ID,
					ActionObject = actionObject,
					ActionObjectID = actionObjectId,
					Action = action,
					ObjectName = ft.FriendlyName,
					ActionObjectName = actionObjectName,
					ActionObjectTypeName = "Field Type",
					ActionDescription = $"This field type has been {action.ToLowerInvariant()}",
					Date = DateTime.UtcNow,
					ResourceID = ft.UpdatedBy,
					AuditFields = new List<AuditField>()
				};
			}


			var currentVersion = CompanyContext.Audits.Where(x => x.ActionObject == Audit.Object && x.ActionObjectID == Audit.ObjectID).OrderByDescending(x => x.Version).Select(x => x.Version).FirstOrDefault();
			Audit.Version = currentVersion + 1;

			if (entry.State != EntityState.Deleted)
			{
				HandleFieldUpdates(entry, entry.Entity);
			}
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

				if (oldValue != newValue)
				{
					Audit.AuditFields.Add(
						new AuditField()
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
			if (entry.Entity is AssetType)
			{
				AssetType o = entry.Entity as AssetType;

				Audit.ObjectID = o.ObjectID;
				Audit.ActionObjectID = o.ObjectID;
			}

			if (entry.Entity is FieldType)
			{
				FieldType ft = entry.Entity as FieldType;

				Audit.ObjectID = ft.ID;
			}
		}
	}

	public class EFChangeTracker
	{
		private List<EFEntryState> _trackedChanges = new List<EFEntryState>();
		private CompanyContext ctx;
		public EFChangeTracker(CompanyContext ctx)
		{
			this.ctx = ctx;
		}
		public void Add(ObjectStateEntry entry)
		{
			_trackedChanges.Add(new EFEntryState(entry, ctx));
		}

		public void SaveChangeLogs()
		{
			List<Audit> audits = new List<Audit>();

			_trackedChanges.ForEach(x => x.UpdateIds());
			_trackedChanges.ForEach(x => audits.Add(x.Audit));

			if (audits.Count > 0)
			{
				ctx.Audits.AddRange(audits);
				ctx.SaveChanges();
			}
		}
	}
}