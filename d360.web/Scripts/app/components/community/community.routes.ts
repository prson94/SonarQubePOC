import * as community from './index'

export const CommunityRoutes = [
    {
        path: 'a/community',
        component: community.CommunityComponent,
        children: [
            { path: '', component: community.CommunitySummaryComponent }
        ]
    }
];