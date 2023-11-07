using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities.Metric
{
	[Table("ExternalMeasureResult", Schema = "metrics")]
	public class ExternalMeasureResult
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }
		public Guid AssetUid { get; set; }
		public Guid AssetVersionUid { get; set; }
		public DateTime EffectiveDate { get; set; }
		public bool Value { get; set; }
	}
}
