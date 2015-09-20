using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.model;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using d360.core.entities;
using System.Data;
using d360.core.enums;
using d360.core.entities.Views;

namespace d360.model.interfaces
{
    public interface IStatusUpdateRepository : IRepository<StatusUpdate, long>
    {
        IQueryable<VerboseStatusUpdate> GetVerboseByType(SystemObjects type, int id, int skip = 0, int take = 25);
        IQueryable<StatusUpdate> GetByType(SystemObjects type, int id, int skip = 0, int take = 25);

        IQueryable<VerboseStatusUpdate> Create(string type, int id, StatusUpdate o);

        VerboseStatusUpdate GetVerboseStatusUpdateById(long id);

        IQueryable<VerboseStatusUpdateCategory> GetStatusUpdateCategoriesByFollower(int id);
        IQueryable<VerboseStatusUpdate> GetStatusUpdatesByFollower(int id, int skip = 0, int take = 25);
        IQueryable<VerboseStatusUpdate> GetStatusUpdatesByPublicGroups(int skip = 0, int take = 25);
    }

    public interface IStatusUpdateCommentRepository : IRepository<StatusUpdateComment, long>
    {
        VerboseStatusUpdateComment CreateComment(StatusUpdateComment o);
        IQueryable<VerboseStatusUpdateComment> Get(long id, int skip = 0, int take = 50);
        VerboseStatusUpdateComment GetVerboseById(long id);
    }
}
