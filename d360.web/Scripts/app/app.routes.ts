import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";
import { FeatureFlagGuard } from "./guards/feature-flag.guard";
import { RedirectGuard } from "./guards/redirect.guard";
import { AdminComponent } from "./components/admin/admin.component";
import { AdminUserGuard } from "./guards/admin-user.guard";


const routes: Routes = [
	{
		path: "",
		children: [],
		canActivate: [RedirectGuard],
		pathMatch: "full"
	},

	// lazy loaded modules
	{
		path: 'admin',
		component: AdminComponent,
		canActivate: [AdminUserGuard],
		children: [
			{ path: 'configuration/assets', loadChildren: () => import('./components/admin/asset-type-configuration/asset-type-configuration.module').then((m) => m.AssetTypeConfigurationModule) },
			{ path: 'relationships', loadChildren: () => import('./components/admin/relationships/admin-relationships.module').then((m) => m.AdminRelationshipsModule) },
			{ path: "relationships/:assetTypeUid/fields", data: { type: 'relationship' }, loadChildren: () => import("./components/sidebar/fields/fields.module").then((m) => m.FieldsModule) },
			{ path: "relationships/:uid/details", data: { type: 'relationship' }, loadChildren: () => import("./components/admin/relationships/detail-page/relationship-type-detail-page.module").then((m) => m.RelationshipTypeDetailPageModule) },
			{ path: "relationships/:uid/log", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
			{ path: 'workflow', loadComponent: () => import('./pages/workflow-types/index').then((m) => m.WorkflowTypesIndex) },
			{ path: 'load', loadChildren: () => import('./components/admin/load/admin-load.module').then((m) => m.AdminLoadModule) },
			{ path: 'settings', loadComponent: () => import('./pages/settings/index').then((m) => m.SettingsIndex) },
			{ path: "scoring/:uid/log", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
			{ path: 'scoring', loadChildren: () => import('./components/admin/scoring/admin-scoring.module').then((m) => m.AdminScoringModule) },
			{ path: 'dashboard', loadChildren: () => import('./components/admin/dashboards/admin-dashboards.module').then((m) => m.AdminDashboardsModule), canActivate: [FeatureFlagGuard] },
			{ path: 'security', loadChildren: () => import('./components/admin/security/security.module').then((m) => m.AdminSecurityModule) }, //, canActivate: [FeatureFlagGuard]
			{ path: 'responsibilities', loadChildren: () => import('./components/admin/responsibilities/admin-responsibilities.module').then((m) => m.AdminResponsibilitiesModule) },
			{ path: "responsibilities/:uid/log", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
			{ path: 'resources', loadChildren: () => import('./components/admin/resources/admin-resources.module').then((m) => m.AdminResourcesModule) },
			{ path: "resources/:uid/log", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
			{ path: 'groups', loadChildren: () => import('./components/admin/groups/_module').then((m) => m.AdminGroupsModule) },
			{ path: "groups/:uid/log", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
			{ path: 'configuration/WorkflowActions', loadChildren: () => import('./components/admin/issuetypes/admin-issue-types.module').then((m) => m.AdminIssueTypesModule) },
			{ path: "predicate", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
			{ path: 'predicates', loadChildren: () => import('./components/admin/predicates/admin-predicates.module').then((m) => m.AdminPredicatesModule) },
			{ path: 'exporttemplates', loadChildren: () => import('./components/admin/exporttemplates/admin-export-templates.module').then((m) => m.AdminExportTemplatesModule) },
			{ path: 'tags', loadChildren: () => import('./components/admin/tags/admin-tags.module').then((m) => m.AdminTagsModule) },
			{ path: 'branding', loadChildren: () => import('./components/admin/branding/admin-branding.module').then((m) => m.AdminBrandingModule) },
		]
	},
	{ path: "assettype", loadChildren: () => import("./components/asset/asset.module").then((m) => m.AssetModule), data: { preload: false } },
	{ path: "assets", loadChildren: () => import("./components/assets-base/assets-base.module").then((m) => m.AssetsBaseModule), data: { preload: false } },
	{ path: "assets", loadChildren: () => import("./components/reference/reference.module").then((m) => m.ReferenceModule) },
	{ path: "community", loadComponent: () => import("./pages/community/index").then((c) => c.CommunityIndex) },
	{ path: "help", loadChildren: () => import("./components/help/help.module").then((m) => m.HelpModule) },
	//{ path: "admin", loadChildren: () => import("./components/admin/admin.module").then((m) => m.AdminModule) },
	{ path: "monitor", loadChildren: () => import("./components/monitor/monitor.module").then((m) => m.MonitorModule), canActivate: [FeatureFlagGuard] },
	{ path: "reference", loadChildren: () => import("./components/reference-v2/reference-v2.module").then((m) => m.ReferenceV2Module), canActivate: [FeatureFlagGuard] },
	{ path: "quality/rule", loadChildren: () => import("./components/rule/rule.module").then((m) => m.RuleModule) },
	{ path: "tag", loadChildren: () => import("./components/tag/tag.module").then((m) => m.TagModule) },
	{ path: "connectorLabel", loadChildren: () => import("./components/connector-label/connector-label.module").then((m) => m.ConnectorLabelModule) },
	{ path: "group", loadChildren: () => import("./components/group/group.module").then((m) => m.GroupModule) },
	{ path: "home", loadComponent: () => import("./pages/home/index").then((c) => c.HomeIndex) },
	{ path: "gallery", loadChildren: () => import("./components/gallery/gallery.module").then((m) => m.GalleryModule) },
	{ path: "search", loadChildren: () => import("./components/search/search.module").then((m) => m.SearchModule) },
	{ path: "workflow", loadChildren: () => import("./components/workflow/workflow.module").then((m) => m.WorkflowModule) },
	{ path: "dashboard", loadChildren: () => import("./components/sidebar/dashboard/dashboard.module").then((m) => m.DashboardModule) },
	{ path: "sidebar/itemfollow", loadChildren: () => import("./components/sidebar/itemfollow/itemfollow.module").then((m) => m.ItemFollowModule) },
	{ path: "sidebar/itemown", loadChildren: () => import("./components/sidebar/itemown/itemown.module").then((m) => m.ItemOwnModule) },
	{ path: "users", loadChildren: () => import("./components/resource/resource.module").then((m) => m.ResourceModule) },
	{ path: "users", loadChildren: () => import("./components/sidebar/membergroup/membergroup.module").then((m) => m.MemberGroupModule) },
	//sidebar
	{ path: "assets", loadChildren: () => import("./components/sidebar/permissions/permissions.module").then((m) => m.PermissionsModule) },
	{ path: "asset", loadChildren: () => import("./components/asset/asset.module").then((m) => m.AssetModule), data: { preload: false } },
	{ path: "assets/:assetTypeUid/fields", loadChildren: () => import("./components/sidebar/fields/fields.module").then((m) => m.FieldsModule) },
	{ path: "semantics/:uid/log", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
	{ path: "semantics", loadChildren: () => import("./components/semantic/semantics.module").then((m) => m.SemanticsModule) },
	{ path: "tag/:uid/log", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
	{ path: "assignments", loadChildren: () => import("./components/assignments/assignments.module").then((m) => m.AssignmentsModule), canActivate: [FeatureFlagGuard] },
	{ path: "assignmentDetails", loadChildren: () => import("./components/assignments/assignments.module").then((m) => m.AssignmentsModule) },
	{ path: "requests", loadChildren: () => import("./components/assignments/assignments.module").then((m) => m.AssignmentsModule), canActivate: [FeatureFlagGuard] },
	{ path: "tag", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
	{ path: "dataCatalog", loadChildren: () => import("./components/data-catalog/data-catalog.module").then((m) => m.DataCatalogModule) },

	{
		path: "**",
		canActivate: [RedirectGuard],
		children: [],
	},

];

@NgModule({
	imports: [RouterModule.forRoot(routes, { onSameUrlNavigation: "reload" })],
	exports: [RouterModule],
})
export class AppRoutingModule { }
