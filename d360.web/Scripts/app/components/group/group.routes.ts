import { GroupComponent } from './group.component';
import { GroupItemComponent } from './group-item.component';
import { GroupListComponent } from './group-list.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const GroupRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_GROUP_ROOT,
        component: GroupComponent,
        children: [
            { path: ':groupId', component: GroupItemComponent },
            { path: '', component: GroupListComponent }
        ]
    }
];