import { FusionComponent } from './fusion.component';
import { FusionItemComponent } from './fusion-item.component';
import { FusionListComponent } from './fusion-list.component';
import { FusionAttributeItemComponent } from './fusion-attribute-item.component';

export const FusionRoutes = [
    {
        path: 'a/fusion',
        component: FusionComponent,        
        children: [
            { path: ':fusionId', component: FusionItemComponent },            
            { path: '', component: FusionListComponent },
            { path: 'fusionattribute/:fusionAttributeTypeId/:fusionAttributeId', component: FusionAttributeItemComponent },
        ]
    }
];