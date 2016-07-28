import * as policy from './index'

export const PolicyRoutes = [
    {
        path: 'a/policy',
        component: policy.PolicyComponent,
        children: [
            { path: ':policyId', component: policy.PolicyItemComponent }
        ]
    }
];