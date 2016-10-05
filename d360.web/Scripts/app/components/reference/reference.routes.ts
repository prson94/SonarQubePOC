import { ReferenceComponent } from './reference.component';
import { ReferenceListComponent } from './reference-list.component';

export const ReferenceRoutes = [
    {
        path: 'a/reference',
        component: ReferenceComponent,        
        children: [
            { path: '', component: ReferenceListComponent },            
        ]
    }
];