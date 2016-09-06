import { SearchComponent} from './search.component';
import { RouterModule } from '@angular/router';

export const SearchRoutes = [
    { path: 'a/search', component: SearchComponent }
];

export const routing = RouterModule.forChild(SearchRoutes);