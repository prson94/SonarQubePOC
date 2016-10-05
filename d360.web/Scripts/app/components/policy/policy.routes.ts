import * as policy from './index'

export const PolicyRoutes = [
    {
        path: 'a/policy',
        component: policy.PolicyComponent,
        children: [
            { path: ':policyTypeId', component: policy.PolicyItemComponent },
            { path: ':policyTypeId/structure', component: policy.PolicyItemStructureComponent },
        ]
    }
];