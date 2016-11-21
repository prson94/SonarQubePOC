import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HomeRoutes} from './components/home/home.routes';
import { ArtifactRoutes } from './components/artifact/artifact.routes';
import { ModelRoutes } from './components/model/model.routes';
import { ReferenceRoutes } from './components/reference/reference.routes';
import { SearchRoutes } from './components/search/search.routes';
import { WorkflowRoutes } from './components/workflow/workflow.routes';
import { SiteUrlHelpers } from './static/site-url-helpers';

const routes: Routes = [    
    { path: SiteUrlHelpers.SITE_URL_PREFIX, redirectTo: SiteUrlHelpers.SITE_URL_HOME_ROOT, pathMatch: 'full' },  
    ...HomeRoutes,
    ...ArtifactRoutes,    
    ...ModelRoutes,          
    ...ReferenceRoutes,    
    ...SearchRoutes,    
    ...WorkflowRoutes,   
    // lazy loaded modules 
    { path: SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT, loadChildren: './components/community/community.module#CommunityModule?chunkName=communityChunk' },            
    { path: SiteUrlHelpers.SITE_URL_HELP_ROOT, loadChildren: './components/help/help.module#HelpModule?chunkName=helpChunk' },
    { path: SiteUrlHelpers.SITE_URL_ADMIN_ROOT, loadChildren: './components/admin/admin.module#AdminModule?chunkName=adminChunk' },   
    { path: SiteUrlHelpers.SITE_URL_FUSION_ROOT, loadChildren: './components/fusion/fusion.module#FusionModule?chunkName=fusionChunk' },   
    { path: SiteUrlHelpers.SITE_URL_MONITOR_ROOT, loadChildren: './components/monitor/monitor.module#MonitorModule?chunkName=monitorChunk' },   
    { path: SiteUrlHelpers.SITE_URL_RULE_ROOT, loadChildren: './components/rule/rule.module#RuleModule?chunkName=ruleChunk' },   
    { path: SiteUrlHelpers.SITE_URL_GROUP_ROOT, loadChildren: './components/group/group.module#GroupModule?chunkName=groupChunk' },  
    { path: SiteUrlHelpers.SITE_URL_POLICY_ROOT, loadChildren: './components/policy/policy.module#PolicyModule?chunkName=policyChunk' }, 
    { path: SiteUrlHelpers.SITE_URL_RESOURCE_ROOT, loadChildren: './components/resource/resource.module#ResourceModule?chunkName=resourceChunk' }, 
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule],
})
export class AppRoutingModule { }