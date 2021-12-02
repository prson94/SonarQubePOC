namespace d360.model.DataAccessLayer.repositories
{
    internal abstract class DapperRepositoryBase<TConnectionProvider>
        where TConnectionProvider: IDbConnectionProvider
    {
        protected IDapperQueryComposer<TConnectionProvider> QueryComposer { get; }

        protected DapperRepositoryBase(IDapperQueryComposer<TConnectionProvider> queryComposer)
        {
            QueryComposer = queryComposer;
        }
    }
}