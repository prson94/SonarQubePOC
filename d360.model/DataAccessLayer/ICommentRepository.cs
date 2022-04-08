using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;
using d360.core.enums;

namespace d360.model.DataAccessLayer
{
    public interface ICommentRepository
    {
        Task<CommentDetail> AddComment(CommentApiPostModel comment, CommentType commentType = CommentType.Social);
        
        bool AddVote(Guid commentUid, int resourceId, Emoji emoji, bool toggle = true);
        
        bool DeleteComment(Guid commentUid);
        
        bool DeleteVote(Guid commentUid, int resourceId, Emoji emoji);
        
        Task<CommentDetail> EditComment(Guid commentUid, CommentApiPutModel comment);
        
        Task<List<CommentCount>> GetCommentCountsByFollower(int resourceId, string searchPhrase = null, DateTime? rangeStart = null, DateTime? rangeEnd = null);
        
        Task<CommentDetail> GetCommentDetailByUid(Guid commentUid);
        
        Task<List<CommentVoteDetail>> GetCommentVotesByCommentUid(Guid commentUid);
        
        Task<List<CommentVoterDetail>> GetCommentVotersByCommentAndEmoji(Guid commentUid, Emoji emoji);
        
        Task<CommentDetails> GetCommentDetails(IEnumerable<KeyValuePair<string, string>> queryParams);
    }
}
