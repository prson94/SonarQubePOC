import * as reference from './index'

export const ReferenceRoutes = [
    {
        path: 'a/reference',
        component: reference.ReferenceComponent,
        children: [
            { path: '', component: reference.ReferenceListComponent },            
        ]
    }
];