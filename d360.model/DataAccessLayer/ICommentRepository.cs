using d360.core.entities;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface ICommentRepository
    {
        Task<CommentDetail> AddComment(CommentApiPostModel comment);
        bool AddVote(Guid commentUid, int resourceId, Emoji emoji);
        bool DeleteComment(Guid commentUid);
        bool DeleteVote(Guid commentUid, int resourceId, Emoji emoji);
        Task<CommentDetail> EditComment(Guid commentUid, CommentApiPutModel comment);
        Task<List<CommentCount>> GetCommentCountsByFollower(int resourceId, string searchPhrase = null, DateTime? rangeStart = null, DateTime? rangeEnd = null);
        Task<CommentDetail> GetCommentDetailByUid(Guid commentUid);
    }
}
