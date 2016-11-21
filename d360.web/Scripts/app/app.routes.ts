import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HomeRoutes} from './components/home/home.routes';
import { ArtifactRoutes } from './components/artifact/artifact.routes';
import { ModelRoutes } from './components/model/model.routes';
import { PolicyRoutes } from './components/policy/policy.routes';
import { FusionRoutes } from './components/fusion/fusion.routes';
import { ResourceRoutes } from './components/resource/resource.routes';
import { RuleRoutes } from './components/rule/rule.routes';
import { MonitorRoutes } from './components/monitor/monitor.routes';
import { ReferenceRoutes } from './components/reference/reference.routes';
import { SearchRoutes } from './components/search/search.routes';
import { GroupRoutes } from './components/group/group.routes';
import { WorkflowRoutes } from './components/workflow/workflow.routes';
import { SiteUrlHelpers } from './static/site-url-helpers';

const routes: Routes = [    
    { path: SiteUrlHelpers.SITE_URL_PREFIX, redirectTo: SiteUrlHelpers.SITE_URL_HOME_ROOT, pathMatch: 'full' },  
    ...HomeRoutes,
    ...ArtifactRoutes,
    ...GroupRoutes,    
    ...ModelRoutes,
    ...PolicyRoutes,
    ...FusionRoutes,
    ...ResourceRoutes,
    ...RuleRoutes,
    ...MonitorRoutes,   
    ...ReferenceRoutes,    
    ...SearchRoutes,    
    ...WorkflowRoutes,   
    // lazy loaded modules 
    { path: SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT, loadChildren: './components/community/community.module#CommunityModule' },            
    { path: SiteUrlHelpers.SITE_URL_HELP_ROOT, loadChildren: './components/help/help.module#HelpModule' },
    { path: SiteUrlHelpers.SITE_URL_ADMIN_ROOT, loadChildren: './components/admin/admin.module#AdminModule' },   
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule],
})
export class AppRoutingModule { }