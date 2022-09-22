import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { RedirectGuard } from "./guards/redirect.guard";


const routes: Routes = [
    {
        path: "",
        children: [], 
        canActivate: [RedirectGuard],
        pathMatch: "full"
    },

    // lazy loaded modules 
    { path: "assettype", loadChildren: () => import("./components/asset/asset.module").then((m) => m.AssetModule), data: { preload: false } },
	{ path: "assets", loadChildren: () => import("./components/assets-base/assets-base.module").then((m) => m.AssetsBaseModule), data: { preload: false } },
    { path: "assets", loadChildren: () => import("./components/reference/reference.module").then((m) => m.ReferenceModule) },
    { path: "community", loadChildren: () => import("./components/community/community.module").then((m) => m.CommunityModule) },
    { path: "help", loadChildren: () => import("./components/help/help.module").then((m) => m.HelpModule) },
    { path: "admin", loadChildren: () => import("./components/admin/admin.module").then((m) => m.AdminModule) },
    { path: "monitor", loadChildren: () => import("./components/monitor/monitor.module").then((m) => m.MonitorModule) },
    { path: "quality/rule", loadChildren: () => import("./components/rule/rule.module").then((m) => m.RuleModule) },
    { path: "tag", loadChildren: () => import("./components/tag/tag.module").then((m) => m.TagModule) },
    { path: "connectorLabel", loadChildren: () => import("./components/connector-label/connector-label.module").then((m) => m.ConnectorLabelModule) },
    { path: "group", loadChildren: () => import("./components/group/group.module").then((m) => m.GroupModule) },
    { path: "home", loadChildren: () => import("./components/home/home.module").then((m) => m.HomeModule) },
    { path: "gallery", loadChildren: () => import("./components/gallery/gallery.module").then((m) => m.GalleryModule) },
    { path: "search", loadChildren: () => import("./components/search/search.module").then((m) => m.SearchModule) },
    { path: "workflow", loadChildren: () => import("./components/workflow/workflow.module").then((m) => m.WorkflowModule) },
    { path: "dashboard", loadChildren: () => import("./components/sidebar/dashboard/dashboard.module").then((m) => m.DashboardModule) },
    { path: "cart", loadChildren: () => import("./components/shoppingcart/shopping-cart.module").then((m) => m.ShoppingCartModule) },
    { path: "sidebar/itemfollow", loadChildren: () => import("./components/sidebar/itemfollow/itemfollow.module").then((m) => m.ItemFollowModule) },
    { path: "sidebar/governanceRoles", loadChildren: () => import("./components/sidebar/governance-roles/governance-roles-sidebar.module").then((m) => m.GovernanceRolesModule) },
    { path: "sidebar/connectorLabels", loadChildren: () => import("./components/sidebar/connector-labels/connector-labels-sidebar.module").then((m) => m.ConnectorLabelsModule) },
	{ path: "sidebar/itemown", loadChildren: () => import("./components/sidebar/itemown/itemown.module").then((m) => m.ItemOwnModule) },
	{ path: "semantics", loadChildren: () => import("./components/semantic/semantics.module").then((m) => m.SemanticsModule) },   
    { path: "users", loadChildren: () => import("./components/resource/resource.module").then((m) => m.ResourceModule) },
    { path: "users", loadChildren: () => import("./components/sidebar/membergroup/membergroup.module").then((m) => m.MemberGroupModule) },
    { path: "asset", loadChildren: () => import("./components/asset/asset.module").then((m) => m.AssetModule), data: { preload: false } },
	//sidebar
    { path: "assets", loadChildren: () => import("./components/sidebar/permissions/permissions.module").then((m) => m.PermissionsModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/children/children.module").then((m) => m.ChildrenModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/score/score.module").then((m) => m.ScoreModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/comments/comments.module").then((m) => m.CommentsModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/actions/actions.module").then((m) => m.ActionsModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/workflowmonitor/workflow-monitor.module").then((m) => m.WorkflowMonitorModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/followers/followers.module").then((m) => m.FollowersModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/ruleresults/rule-results.module").then((m) => m.RuleResultsModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/ownership/ownership.module").then((m) => m.OwnershipModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/visualization/visualization.module").then((m) => m.VisualizationModule) },
    { path: "asset", loadChildren: () => import("./components/sidebar/relationships/relationships.module").then((m) => m.RelationshipsModule) },
	{ path: "assets", loadChildren: () => import("./components/sidebar/fields/fields.module").then((m) => m.FieldsModule) },
    { path: "admin/predicate", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
	{ path: "semantics", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
    { path: "tag", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
	{ path: "admin/relationships", data: { type: 'relationship' }, loadChildren: () => import("./components/sidebar/fields/fields.module").then((m) => m.FieldsModule) },

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