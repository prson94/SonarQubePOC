using System.Data;

namespace d360.model.DataAccessLayer
{
    public interface IDbConnectionProvider
    {
        IDbConnection Connection { get; }
    }
}