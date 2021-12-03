using Autofac;
using d360.model.DataAccessLayer;
using d360.model.DataAccessLayer.repositories;
using d360.model.validators;

namespace d360.model
{
    public static class DependencyInjectionExtensions
    {
        public static void RegisterModelModule(this ContainerBuilder builder)
        {
            builder.RegisterType<CompanyDbConnectionProvider>().As<ICompanyDbConnectionProvider>().InstancePerRequest();
            builder.RegisterType<ResponsibilityDapperRepository>().As<IResponsibilityDapperRepository>().InstancePerRequest();
            builder.RegisterType<FavoritesRepository>().AsImplementedInterfaces().InstancePerRequest();
            builder.RegisterType<ResponsibilityTypeRepository>().As<IResponsibilityTypeRepository>().InstancePerRequest();

            builder.RegisterGeneric(typeof(DapperQueryComposer<>)).As(typeof(IDapperQueryComposer<>)).InstancePerRequest();

            builder.RegisterType<WorkflowApiModelValidator>().As<IWorkflowApiModelValidator>().InstancePerRequest();
            builder.RegisterType<SurveyApiModelValidator>().As<ISurveyApiModelValidator>().InstancePerRequest();

            builder.RegisterType<CommunityContext>().As<ICommunityContext>().InstancePerRequest();
            builder.RegisterType<CompanyContext>().As<ICompanyContext>().InstancePerRequest();

            builder.RegisterType<AssetRepository>().As<IAssetRepository>().InstancePerRequest();
            builder.RegisterType<CommentRepository>().As<ICommentRepository>().InstancePerRequest();
            builder.RegisterType<CrossReferencesRepository>().As<ICrossReferencesRepository>().InstancePerRequest();
            builder.RegisterType<TagRepository>().As<ITagRepository>().InstancePerRequest();
            builder.RegisterType<FieldsRepository>().As<IFieldsRepository>().InstancePerRequest();
            builder.RegisterType<WorkflowRepository>().As<IWorkflowRepository>().InstancePerRequest();
            builder.RegisterType<ResourceRepository>().As<IResourceRepository>().InstancePerRequest();
            builder.RegisterType<IssueRepository>().As<IIssueRepository>().InstancePerRequest();
            builder.RegisterType<RelationshipRepository>().As<IRelationshipRepository>().InstancePerRequest();
            builder.RegisterType<MetricsRepository>().As<IMetricsRepository>().InstancePerRequest();
            builder.RegisterType<ResponsibilityRepository>().As<IResponsibilityRepository>().InstancePerRequest();
            builder.RegisterType<SettingsRepository>().As<ISettingsRepository>().InstancePerRequest();
            builder.RegisterType<SurveyRepository>().As<ISurveyRepository>().InstancePerRequest();
            builder.RegisterType<MembershipRepository>().As<IMembershipRepository>().InstancePerRequest();
            builder.RegisterType<ScoringRepository>().As<IScoringRepository>().InstancePerRequest();
            builder.RegisterType<GraphFilterRepository>().As<IGraphFilterRepository>().InstancePerRequest();
            builder.RegisterType<ProcessRepository>().As<IProcessRepository>().InstancePerRequest();
            builder.RegisterType<ConnectorLabelRepository>().As<IConnectorLabelRepository>().InstancePerRequest();
            builder.RegisterType<DataProfileRepository>().As<IDataProfileRepository>().InstancePerRequest();
        }
    }
}
