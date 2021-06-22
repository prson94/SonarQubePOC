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
    { path: "community", loadChildren: () => import("./components/community/community.module").then((m) => m.CommunityModule) },
    { path: "help", loadChildren: () => import("./components/help/help.module").then((m) => m.HelpModule) },
    { path: "admin", loadChildren: () => import("./components/admin/admin.module").then((m) => m.AdminModule) },
    { path: "monitor", loadChildren: () => import("./components/monitor/monitor.module").then((m) => m.MonitorModule) },
    { path: "quality/rule", loadChildren: () => import("./components/rule/rule.module").then((m) => m.RuleModule) },
    { path: "tag", loadChildren: () => import("./components/tag/tag.module").then((m) => m.TagModule) },
    { path: "connectorLabel", loadChildren: () => import("./components/connector-label/connector-label.module").then((m) => m.ConnectorLabelModule) },
    { path: "group", loadChildren: () => import("./components/group/group.module").then((m) => m.GroupModule) },
    { path: "policy", data: { type: "policy" }, loadChildren: () => import("./components/hierarchy/hierarchy.module").then((m) => m.HierarchyModule) },
    { path: "model", data: { type: "model" }, loadChildren: () => import("./components/hierarchy/hierarchy.module").then((m) => m.HierarchyModule) },
    { path: "resource", loadChildren: () => import("./components/resource/resource.module").then((m) => m.ResourceModule) },
    { path: "reference", loadChildren: () => import("./components/reference/reference.module").then((m) => m.ReferenceModule) },
    { path: "asset", loadChildren: () => import("./components/asset/asset.module").then((m) => m.AssetModule), data: { preload: false } },
    { path: "assettype", loadChildren: () => import("./components/asset/asset.module").then((m) => m.AssetModule), data: { preload: false } },
    { path: "assets", loadChildren: () => import("./components/assets/assets.module").then((m) => m.AssetsModule), data: { preload: false } },
    { path: "artifact", loadChildren: () => import("./components/artifact/artifact.module").then((m) => m.ArtifactModule), data: { preload: false } },
    { path: "home", loadChildren: () => import("./components/home/home.module").then((m) => m.HomeModule) },
    { path: "gallery", loadChildren: () => import("./components/gallery/gallery.module").then((m) => m.GalleryModule) },
    { path: "search", loadChildren: () => import("./components/search/search.module").then((m) => m.SearchModule) },
    { path: "workflow", loadChildren: () => import("./components/workflow/workflow.module").then((m) => m.WorkflowModule) },
    { path: "sidebar/audit", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
    { path: "dashboard", loadChildren: () => import("./components/sidebar/dashboard/dashboard.module").then((m) => m.DashboardModule) },
    { path: "sidebar/followers", loadChildren: () => import("./components/sidebar/followers/followers.module").then((m) => m.FollowersModule) },
    { path: "sidebar/ownership", loadChildren: () => import("./components/sidebar/ownership/ownership.module").then((m) => m.OwnershipModule) },
    { path: "sidebar/visualization", loadChildren: () => import("./components/sidebar/visualization/visualization.module").then((m) => m.VisualizationModule) },
    { path: "sidebar/relationships", loadChildren: () => import("./components/sidebar/relationships/relationships.module").then((m) => m.RelationshipsModule) },
    { path: "sidebar/children", loadChildren: () => import("./components/sidebar/children/children.module").then((m) => m.ChildrenModule) },
    { path: "sidebar/workflowmonitor", loadChildren: () => import("./components/sidebar/workflowmonitor/workflow-monitor.module").then((m) => m.WorkflowMonitorModule) },
    { path: "sidebar/fields", loadChildren: () => import("./components/sidebar/fields/fields.module").then((m) => m.FieldsModule) },
    { path: "sidebar/responsibilities", loadChildren: () => import("./components/sidebar/permissions/permissions.module").then((m) => m.PermissionsModule) },
    { path: "cart", loadChildren: () => import("./components/shoppingcart/shopping-cart.module").then((m) => m.ShoppingCartModule) },
    { path: "sidebar/itemfollow", loadChildren: () => import("./components/sidebar/itemfollow/itemfollow.module").then((m) => m.ItemFollowModule) },
    { path: "sidebar/itemown", loadChildren: () => import("./components/sidebar/itemown/itemown.module").then((m) => m.ItemOwnModule) },
    { path: "sidebar/membergroup", loadChildren: () => import("./components/sidebar/membergroup/membergroup.module").then((m) => m.MemberGroupModule) },
    { path: "sidebar/comments", loadChildren: () => import("./components/sidebar/comments/comments.module").then((m) => m.CommentsModule) },
    { path: "sidebar/workflowmonitor", loadChildren: () => import("./components/workflowmonitor/workflowmonitor.module").then((m) => m.WorkflowMonitorModule) },
    { path: "sidebar/score", loadChildren: () => import("./components/sidebar/score/score.module").then((m) => m.ScoreModule) },
    { path: "sidebar/actions", loadChildren: () => import("./components/sidebar/actions/actions.module").then((m) => m.ActionsModule) },
    { path: "sidebar/ruleResults", loadChildren: () => import("./components/sidebar/ruleresults/rule-results.module").then((m) => m.RuleResultsModule) },
    { path: "sidebar/governanceRoles", loadChildren: () => import("./components/sidebar/governance-roles/governance-roles-sidebar.module").then((m) => m.GovernanceRolesModule) },
    { path: "sidebar/connectorLabels", loadChildren: () => import("./components/sidebar/connector-labels/connector-labels-sidebar.module").then((m) => m.ConnectorLabelsModule) },    
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