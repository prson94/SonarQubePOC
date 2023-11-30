using System;
using System.Collections.Generic;

namespace d360.core.queue
{
	public enum ScoreQueueChangeType
    {
        AssetMeasures = 0,
        MeasureChanged = 3,
        MeasureRemoved = 4,
        RollupPathChanged = 5,
        WorkflowCheck = 6,
        CheckTypeDependencyRemoved = 7,
        RuleAssetRemoved = 8,
		RescoreRequest = 9,
		PatchCatalogExecution = 10
	}

    public class ScoreQueueInfo
    {
        public int CompanyID { get; set; }

        public int? ResourceID { get; set; }

		public int? ExecutionId { get; set; }

		public DateTime StartedOn { get; set; }

        public ScoreQueueChangeType ChangeType { get; set; }

		public object Payload { get; set; }
    }

    public class CheckTypeDependencyRemovedModel
    {
        public List<Guid> VersionUids { get; set; }
    }

    public class RollupPathChangedModel
    {
        public int? IntersectTypeId { get; set; }
        
        public int? AssetTypeId { get; set; }
    }

    public class RuleAssetRemovedModel
    {
        public Guid AssetUid { get; set; }
    }
}
