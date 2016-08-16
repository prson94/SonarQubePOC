import { Routes, RouterModule } from '@angular/router';
import { provideRouter, RouterConfig } from '@angular/router';
import { HomeComponent} from './components/index';
import { AdminRoutes} from './components/admin/admin.routes';
import { HomeRoutes} from './components/home/home.routes';
import { ArtifactRoutes } from './components/artifact/artifact.routes';
import { DiagnosticRoutes } from './components/diagnostic/diagnostic.routes';
import { ModelRoutes } from './components/model/model.routes';
import { PolicyRoutes } from './components/policy/policy.routes';
import { FusionRoutes } from './components/fusion/fusion.routes';
import { ResourceRoutes } from './components/resource/resource.routes';
import { RuleRoutes } from './components/rule/rule.routes';
import { MonitorRoutes } from './components/monitor/monitor.routes';
import { CommunityRoutes } from './components/community/community.routes';
import { ReferenceRoutes } from './components/reference/reference.routes';

export const routes: RouterConfig = [
    ...AdminRoutes,
    ...HomeRoutes,
    ...ArtifactRoutes,
    ...DiagnosticRoutes,
    ...ModelRoutes,
    ...PolicyRoutes,
    ...FusionRoutes,
    ...ResourceRoutes,
    ...RuleRoutes,
    ...MonitorRoutes,
    ...CommunityRoutes,
    ...ReferenceRoutes,
];

export const routing = RouterModule.forRoot(routes);