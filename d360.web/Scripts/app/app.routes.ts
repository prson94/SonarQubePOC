import { provideRouter, RouterConfig } from '@angular/router';
import { HomeComponent} from './components/index';
import { AdminRoutes} from './components/admin/admin.routes';
import { HomeRoutes} from './components/home/home.routes';
import { ArtifactRoutes } from './components/artifact/artifact.routes';

export const routes: RouterConfig = [
    ...AdminRoutes,
    ...HomeRoutes,
    ...ArtifactRoutes    
];


export const APP_ROUTER_PROVIDERS = [
    provideRouter(routes)
];