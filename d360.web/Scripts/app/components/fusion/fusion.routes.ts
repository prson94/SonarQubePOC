import * as fusion from './index'

export const FusionRoutes = [
    {
        path: 'a/fusion',
        component: fusion.FusionComponent,        
        children: [
            { path: ':fusionId', component: fusion.FusionItemComponent },
            { path: '', component: fusion.FusionListComponent }
        ]
    }
];