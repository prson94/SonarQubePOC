using d360.core.enums;

namespace repositories.domain.contracts
{
	public class ReadImpactedScoresByExecutionDTO
	{
		public int AssetTypeId { get; set; }
		public ScoreType ScoreType { get; set; }
	}
}
