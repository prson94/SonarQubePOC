import { GroupComponent } from './group.component';
import { GroupItemComponent } from './group-item.component';
import { GroupListComponent } from './group-list.component';

export const GroupRoutes = [
    {
        path: 'a/group',
        component: GroupComponent,
        children: [
            { path: ':groupId', component: GroupItemComponent },
            { path: '', component: GroupListComponent }
        ]
    }
];