import { NgModule } from '@angular/core';
import { Routes, RouterModule, PreloadingStrategy, Route } from '@angular/router';
import { SiteUrlHelpers } from './static/site-url-helpers';
import { SelectivePreloadingStrategy } from './selective-preloading-strategy';

const routes: Routes = [    
    { path: '', redirectTo: SiteUrlHelpers.SITE_URL_HOME_ROOT, pathMatch: 'full' },      
    
    // lazy loaded modules 
    { path: SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT, loadChildren: './components/community/community.module#CommunityModule?chunkName=communityChunk' },            
    { path: SiteUrlHelpers.SITE_URL_HELP_ROOT, loadChildren: './components/help/help.module#HelpModule?chunkName=helpChunk' },
    { path: SiteUrlHelpers.SITE_URL_ADMIN_ROOT, loadChildren: './components/admin/admin.module#AdminModule?chunkName=adminChunk' },   
    { path: SiteUrlHelpers.SITE_URL_FUSION_ROOT, loadChildren: './components/fusion/fusion.module#FusionModule?chunkName=fusionChunk' },   
    { path: SiteUrlHelpers.SITE_URL_MONITOR_ROOT, loadChildren: './components/monitor/monitor.module#MonitorModule?chunkName=monitorChunk' },   
    { path: SiteUrlHelpers.SITE_URL_RULE_ROOT, loadChildren: './components/rule/rule.module#RuleModule?chunkName=ruleChunk'},   
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
];

@NgModule({
    providers: [SelectivePreloadingStrategy],
    imports: [RouterModule.forRoot(routes,
        { preloadingStrategy: SelectivePreloadingStrategy })],
    exports: [RouterModule],
})
export class AppRoutingModule { }