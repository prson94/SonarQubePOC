import { NgModule } from '@angular/core';
import { Routes, RouterModule, Route } from '@angular/router';
import { SiteUrlHelpers } from './static/site-url-helpers';


const routes: Routes = [
    { path: '', redirectTo: SiteUrlHelpers.getDefaultRoute(), pathMatch: 'full' },

    // lazy loaded modules 
    { path: SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT, loadChildren: () => import('./components/community/community.module').then(m => m.CommunityModule) },
    { path: SiteUrlHelpers.SITE_URL_HELP_ROOT, loadChildren: () => import('./components/help/help.module').then(m => m.HelpModule) },
    { path: SiteUrlHelpers.SITE_URL_ADMIN_ROOT, loadChildren: () => import('./components/admin/admin.module').then(m => m.AdminModule) },
    { path: SiteUrlHelpers.SITE_URL_FUSION_ROOT, loadChildren: () => import('./components/fusion/fusion.module').then(m => m.FusionModule) },
    { path: SiteUrlHelpers.SITE_URL_MONITOR_ROOT, loadChildren: () => import('./components/monitor/monitor.module').then(m => m.MonitorModule) },
    { path: SiteUrlHelpers.SITE_URL_RULE_ROOT, loadChildren: () => import('./components/rule/rule.module').then(m => m.RuleModule) },
    { path: SiteUrlHelpers.SITE_URL_TAG_ROOT, loadChildren: () => import('./components/tag/tag.module').then(m => m.TagModule) },
    { path: SiteUrlHelpers.SITE_URL_GROUP_ROOT, loadChildren: () => import('./components/group/group.module').then(m => m.GroupModule) },
    { path: SiteUrlHelpers.SITE_URL_POLICY_ROOT, loadChildren: () => import('./components/policy/policy.module').then(m => m.PolicyModule) },
    { path: SiteUrlHelpers.SITE_URL_RESOURCE_ROOT, loadChildren: () => import('./components/resource/resource.module').then(m => m.ResourceModule) },
    { path: SiteUrlHelpers.SITE_URL_MODEL_ROOT, loadChildren: () => import('./components/model/model.module').then(m => m.ModelModule) },
    { path: SiteUrlHelpers.SITE_URL_REFERENCE_ROOT, loadChildren: () => import('./components/reference/reference.module').then(m => m.ReferenceModule) },
    { path: SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT, loadChildren: () => import('./components/artifact/artifact.module').then(m => m.ArtifactModule), data: { preload: false } },
    { path: SiteUrlHelpers.SITE_URL_HOME_ROOT, loadChildren: () => import('./components/home/home.module').then(m => m.HomeModule) },
    { path: SiteUrlHelpers.SITE_URL_SEARCH_ROOT, loadChildren: () => import('./components/search/search.module').then(m => m.SearchModule) },
    { path: SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT, loadChildren: () => import('./components/workflow/workflow.module').then(m => m.WorkflowModule) },
    { path: SiteUrlHelpers.SITE_URL_MAPPING_ROOT, loadChildren: () => import('./components/mapping/mapping.module').then(m => m.MappingModule) },
    { path: SiteUrlHelpers.SITE_URL_AUDIT_ROOT, loadChildren: () => import('./components/sidebar/audit/audit.module').then(m => m.AuditModule) },
    { path: SiteUrlHelpers.SITE_URL_DASHBOARD_ROOT, loadChildren: () => import('./components/sidebar/dashboard/dashboard.module').then(m => m.DashboardModule) },
    { path: SiteUrlHelpers.SITE_URL_FOLLOWERS_ROOT, loadChildren: () => import('./components/sidebar/followers/followers.module').then(m => m.FollowersModule) },
    { path: SiteUrlHelpers.SITE_URL_OWNERSHIP_ROOT, loadChildren: () => import('./components/sidebar/ownership/ownership.module').then(m => m.OwnershipModule) },
    { path: SiteUrlHelpers.SITE_URL_VISUALIZATION_ROOT, loadChildren: () => import('./components/sidebar/visualization/visualization.module').then(m => m.VisualizationModule) },
    { path: SiteUrlHelpers.SITE_URL_RELATIONSHIP_ROOT, loadChildren: () => import('./components/sidebar/relationships/relationships.module').then(m => m.RelationshipsModule) },
    { path: SiteUrlHelpers.SITE_URL_CHILDREN_ROOT, loadChildren: () => import('./components/sidebar/children/children.module').then(m => m.ChildrenModule) },
    { path: SiteUrlHelpers.SITE_URL_WORKFLOW_MONITOR_ROOT, loadChildren: () => import('./components/sidebar/workflowmonitor/workflow-monitor.module').then(m => m.WorkflowMonitorModule) },
    { path: SiteUrlHelpers.SITE_URL_FIELDS_ROOT, loadChildren: () => import('./components/sidebar/fields/fields.module').then(m => m.FieldsModule) },
    { path: SiteUrlHelpers.SITE_URL_RESPONSIBILITIES_ROOT, loadChildren: () => import('./components/sidebar/permissions/permissions.module').then(m => m.PermissionsModule) },
    { path: SiteUrlHelpers.SITE_URL_SHOPPING_CART_ROOT, loadChildren: () => import('./components/shoppingcart/shopping-cart.module').then(m => m.ShoppingCartModule) },
    { path: SiteUrlHelpers.SITE_URL_ITEM_FOLLOW_ROOT, loadChildren: () => import('./components/sidebar/itemfollow/itemfollow.module').then(m => m.ItemFollowModule) },
    { path: SiteUrlHelpers.SITE_URL_ITEM_OWN_ROOT, loadChildren: () => import('./components/sidebar/itemown/itemown.module').then(m => m.ItemOwnModule) },
    { path: SiteUrlHelpers.SITE_URL_MEMBER_GROUP_ROOT, loadChildren: () => import('./components/sidebar/membergroup/membergroup.module').then(m => m.MemberGroupModule) },
    { path: SiteUrlHelpers.SITE_URL_COMMENTS_ROOT, loadChildren: () => import('./components/sidebar/comments/comments.module').then(m => m.CommentsModule) },
    { path: SiteUrlHelpers.SITE_URL_WORKFLOWMONITOR_ROOT, loadChildren: () => import('./components/workflowmonitor/workflowmonitor.module').then(m => m.WorkflowMonitorModule) },
    { path: SiteUrlHelpers.SITE_URL_SCORE_ROOT, loadChildren: () => import('./components/sidebar/score/score.module').then(m => m.ScoreModule) },
    { path: SiteUrlHelpers.SITE_URL_SURVEY_ROOT, loadChildren: () => import('./components/sidebar/survey/survey.module').then(m => m.SurveyModule) },
    { path: SiteUrlHelpers.SITE_URL_ACTIONS_ROOT, loadChildren: () => import('./components/sidebar/actions/actions.module').then(m => m.ActionsModule) },
];

@NgModule({
    imports: [RouterModule.forRoot(routes, { onSameUrlNavigation: 'reload' })],
    exports: [RouterModule],
})
export class AppRoutingModule { }