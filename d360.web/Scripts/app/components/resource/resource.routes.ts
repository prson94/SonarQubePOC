import * as resource from './index'

export const ResourceRoutes = [
    {
        path: 'a/resource',
        component: resource.ResourceComponent,
        children: [
            { path: ':resourceId', component: resource.ResourceItemComponent }
        ]
    }
];