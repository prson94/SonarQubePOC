import { HomeComponent} from './home.component';
import { RouterModule } from '@angular/router';

export const HomeRoutes = [
    { path: 'a/home', component: HomeComponent }    
];

export const routing = RouterModule.forChild(HomeRoutes);