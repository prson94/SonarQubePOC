import { CommunityComponent } from './community.component';
import { CommunitySummaryComponent } from './community-summary.component';

export const CommunityRoutes = [
    {
        path: 'a/community',
        component: CommunityComponent,
        children: [
            { path: '', component: CommunitySummaryComponent }
        ]
    }
];