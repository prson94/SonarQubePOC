using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core.enums;

namespace repositories
{
	public interface ISocial
	{
		Task<RepositoryResponse<CommentDetail>> AddComment(CommentApiPostModel comment, CommentType commentType = CommentType.Social);

		bool ProcessWithQueue(List<Asset> taggedAssets);

		RepositoryResponse<bool> AddVote(Guid commentUid, int resourceId, Emoji emoji, bool toggle = true);

		RepositoryResponse<bool> DeleteComment(Guid commentUid);

		RepositoryResponse<bool> DeleteVote(Guid commentUid, int resourceId, Emoji emoji);

		Task<(RepositoryResponse<CommentDetail>, List<Asset>)> EditComment(Guid commentUid, CommentApiPutModel comment);

		Task<List<CommentCount>> GetCommentCountsByFollower(int resourceId, string searchPhrase = null, DateTime? rangeStart = null, DateTime? rangeEnd = null);

		Task <RepositoryResponse<CommentDetail>> GetCommentDetailByUid(Guid commentUid);

		Task<RepositoryResponse<List<CommentVoteDetail>>> GetCommentVotesByCommentUid(Guid commentUid);

		Task<RepositoryResponse<List<CommentVoteDetail>>> GetCommentVotersByCommentAndEmoji(Guid commentUid, Emoji emoji);

		Task<RepositoryResponse<CommentDetails>> GetCommentDetails(IEnumerable<KeyValuePair<string, string>> queryParams);
	}
}
