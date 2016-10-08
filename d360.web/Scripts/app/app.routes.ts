import { Routes, RouterModule } from '@angular/router';
import { HomeComponent} from './components/index';
import { AdminRoutes } from './components/admin/admin.routes';
import { HomeRoutes} from './components/home/home.routes';
import { ArtifactRoutes } from './components/artifact/artifact.routes';
import { ModelRoutes } from './components/model/model.routes';
import { PolicyRoutes } from './components/policy/policy.routes';
import { FusionRoutes } from './components/fusion/fusion.routes';
import { ResourceRoutes } from './components/resource/resource.routes';
import { RuleRoutes } from './components/rule/rule.routes';
import { MonitorRoutes } from './components/monitor/monitor.routes';
import { CommunityRoutes } from './components/community/community.routes';
import { ReferenceRoutes } from './components/reference/reference.routes';
import { SearchRoutes } from './components/search/search.routes';
import { GroupRoutes } from './components/group/group.routes';
import { WorkflowRoutes } from './components/workflow/workflow.routes';
import { SiteUrlHelpers } from './static/site-url-helpers';

export const routes: Routes = [    
    { path: SiteUrlHelpers.SITE_URL_PREFIX, redirectTo: SiteUrlHelpers.SITE_URL_HOME_ROOT, pathMatch: 'full' },
    ...AdminRoutes,
    ...HomeRoutes,
    ...ArtifactRoutes,
    ...GroupRoutes,
    ...ModelRoutes,
    ...PolicyRoutes,
    ...FusionRoutes,
    ...ResourceRoutes,
    ...RuleRoutes,
    ...MonitorRoutes,
    ...CommunityRoutes,
    ...ReferenceRoutes,    
    ...SearchRoutes,    
    ...WorkflowRoutes,
];

export const routing = RouterModule.forRoot(routes);