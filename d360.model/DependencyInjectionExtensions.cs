using Autofac;

using d360.model.DataAccessLayer;
using d360.model.DataAccessLayer.repositories;
using d360.model.validators;
using repositories;

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
            builder.RegisterType<AuditRepository>().As<IAuditRepository>().InstancePerRequest();
            builder.RegisterType<ApplicationHealthDapperRepository>().As<IApplicationHealthDapperRepository>().InstancePerRequest();
            builder.RegisterGeneric(typeof(DapperQueryComposer<>)).As(typeof(IDapperQueryComposer<>)).InstancePerRequest();
            builder.RegisterType<WorkflowApiModelValidator>().As<IWorkflowApiModelValidator>().InstancePerRequest();
            builder.RegisterType<AssetRepository>().As<IAssetRepository>().InstancePerRequest();
			builder.RegisterType<ExecutionsRepository>().As<IExecutionsRepository>().InstancePerRequest();
			builder.RegisterType<TagRepository>().As<ITagRepository>().InstancePerRequest();
            builder.RegisterType<FieldsRepository>().As<IFieldsRepository>().InstancePerRequest();
            builder.RegisterType<WorkflowRepository>().As<IWorkflowRepository>().InstancePerRequest();
            builder.RegisterType<ResourceRepository>().As<IResourceRepository>().InstancePerRequest();
            builder.RegisterType<IssueRepository>().As<IIssueRepository>().InstancePerRequest();
            builder.RegisterType<RelationshipRepository>().As<IRelationshipRepository>().InstancePerRequest();
            builder.RegisterType<MetricsRepository>().As<IMetricsRepository>().InstancePerRequest();
            builder.RegisterType<ResponsibilityRepository>().As<IResponsibilityRepository>().InstancePerRequest();
            builder.RegisterType<ScoringRepository>().As<IScoringRepository>().InstancePerRequest();
            builder.RegisterType<ProcessRepository>().As<IProcessRepository>().InstancePerRequest();
            builder.RegisterType<DataProfileRepository>().As<IDataProfileRepository>().InstancePerRequest();
            builder.RegisterType<SemanticsRepository>().As<ISemanticsRepository>().InstancePerRequest();
            builder.RegisterType<ThemeRepository>().As<IThemeRepository>().InstancePerRequest();
            builder.RegisterType<DashboardRepository>().As<IDashboardRepository>().InstancePerRequest();
			builder.RegisterType<ResourceSettingRepository>().As<IResourceSettingRepository>().InstancePerRequest();
			builder.RegisterType<NavigationRepository>().AsImplementedInterfaces().InstancePerRequest();
        }
    }
}
