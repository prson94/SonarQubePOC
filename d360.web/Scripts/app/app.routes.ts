import { NgModule } from '@angular/core';
import { Routes, RouterModule, Route } from '@angular/router';
import { SiteUrlHelpers } from './static/site-url-helpers';


const routes: Routes = [    
    { path: '', redirectTo: SiteUrlHelpers.getDefaultRoute(), pathMatch: 'full' },
    
    // lazy loaded modules 
    { path: SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT, loadChildren: './components/community/community.module#CommunityModule?chunkName=communityChunk' },            
    { path: SiteUrlHelpers.SITE_URL_HELP_ROOT, loadChildren: './components/help/help.module#HelpModule?chunkName=helpChunk' },
    { path: SiteUrlHelpers.SITE_URL_ADMIN_ROOT, loadChildren: './components/admin/admin.module#AdminModule?chunkName=adminChunk' },   
    { path: SiteUrlHelpers.SITE_URL_FUSION_ROOT, loadChildren: './components/fusion/fusion.module#FusionModule?chunkName=fusionChunk' },   
    { path: SiteUrlHelpers.SITE_URL_MONITOR_ROOT, loadChildren: './components/monitor/monitor.module#MonitorModule?chunkName=monitorChunk' },   
    { path: SiteUrlHelpers.SITE_URL_RULE_ROOT, loadChildren: './components/rule/rule.module#RuleModule?chunkName=ruleChunk' },
    { path: SiteUrlHelpers.SITE_URL_TAG_ROOT, loadChildren: './components/tag/tag.module#TagModule?chunkName=tagChunk' },   
    { path: SiteUrlHelpers.SITE_URL_GROUP_ROOT, loadChildren: './components/group/group.module#GroupModule?chunkName=groupChunk' },  
    { path: SiteUrlHelpers.SITE_URL_POLICY_ROOT, loadChildren: './components/policy/policy.module#PolicyModule?chunkName=policyChunk'}, 
    { path: SiteUrlHelpers.SITE_URL_RESOURCE_ROOT, loadChildren: './components/resource/resource.module#ResourceModule?chunkName=resourceChunk'}, 
    { path: SiteUrlHelpers.SITE_URL_MODEL_ROOT, loadChildren: './components/model/model.module#ModelModule?chunkName=modelChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_REFERENCE_ROOT, loadChildren: './components/reference/reference.module#ReferenceModule?chunkName=referenceChunk'},
    { path: SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT, loadChildren: './components/artifact/artifact.module#ArtifactModule?chunkName=artifactChunk', data: { preload: false } }, 
    { path: SiteUrlHelpers.SITE_URL_HOME_ROOT, loadChildren: './components/home/home.module#HomeModule?chunkName=homeChunk'}, 
    { path: SiteUrlHelpers.SITE_URL_SEARCH_ROOT, loadChildren: './components/search/search.module#SearchModule?chunkName=searchChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT, loadChildren: './components/workflow/workflow.module#WorkflowModule?chunkName=workflowChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_MAPPING_ROOT, loadChildren: './components/mapping/mapping.module#MappingModule?chunkName=mappingChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_AUDIT_ROOT, loadChildren: './components/sidebar/audit/audit.module#AuditModule?chunkName=auditChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_DASHBOARD_ROOT, loadChildren: './components/sidebar/dashboard/dashboard.module#DashboardModule?chunkName=dashboardChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_FOLLOWERS_ROOT, loadChildren: './components/sidebar/followers/followers.module#FollowersModule?chunkName=followersChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_OWNERSHIP_ROOT, loadChildren: './components/sidebar/ownership/ownership.module#OwnershipModule?chunkName=ownershipChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_VISUALIZATION_ROOT, loadChildren: './components/sidebar/visualization/visualization.module#VisualizationModule?chunkName=visualizationChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_RELATIONSHIP_ROOT, loadChildren: './components/sidebar/relationships/relationships.module#RelationshipsModule?chunkName=relationshipsChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_CHILDREN_ROOT, loadChildren: './components/sidebar/children/children.module#ChildrenModule?chunkName=childrenChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_WORKFLOW_MONITOR_ROOT, loadChildren: './components/sidebar/workflowmonitor/workflow-monitor.module#WorkflowMonitorModule?chunkName=workflowMonitorChunk' },
    { path: SiteUrlHelpers.SITE_URL_FIELDS_ROOT, loadChildren: './components/sidebar/fields/fields.module#FieldsModule?chunkName=fieldsChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_RESPONSIBILITIES_ROOT, loadChildren: './components/sidebar/permissions/permissions.module#PermissionsModule?chunkName=permissionsSidebarChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_SHOPPING_CART_ROOT, loadChildren: './components/shoppingcart/shopping-cart.module#ShoppingCartModule?chunkName=shoppingCartChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_ITEM_FOLLOW_ROOT, loadChildren: './components/sidebar/itemfollow/itemfollow.module#ItemFollowModule?chunkName=itemfollowChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_ITEM_OWN_ROOT, loadChildren: './components/sidebar/itemown/itemown.module#ItemOwnModule?chunkName=itemownChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_MEMBER_GROUP_ROOT, loadChildren: './components/sidebar/membergroup/membergroup.module#MemberGroupModule?chunkName=membergroupChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_COMMENTS_ROOT, loadChildren: './components/sidebar/comments/comments.module#CommentsModule?chunkName=commentsChunk' },
    { path: SiteUrlHelpers.SITE_URL_WORKFLOWMONITOR_ROOT, loadChildren: './components/workflowmonitor/workflowmonitor.module#WorkflowMonitorModule?chunkName=workflowMonitorChunk' },   
    { path: SiteUrlHelpers.SITE_URL_SCORE_ROOT, loadChildren: './components/sidebar/score/score.module#ScoreModule?chunkName=scoreChunk' },
    { path: SiteUrlHelpers.SITE_URL_SURVEY_ROOT, loadChildren: './components/sidebar/survey/survey.module#SurveyModule?chunkName=surveyChunk' },
    { path: SiteUrlHelpers.SITE_URL_ACTIONS_ROOT, loadChildren: './components/sidebar/actions/actions.module#ActionsModule?chunkName=actionsChunk' }, 
];

@NgModule({    
    imports: [RouterModule.forRoot(routes, { onSameUrlNavigation: 'reload' })],
    exports: [RouterModule],
})
export class AppRoutingModule { }