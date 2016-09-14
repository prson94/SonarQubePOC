import * as model from './index'

export const ModelRoutes = [
    {
        path: 'a/model',
        component: model.ModelComponent,
        children: [
            { path: 'classification/:group', component: model.ModelListComponent },
            { path: 'classification', component: model.ModelListComponent },
            { path: ':modelId', component: model.ModelItemComponent }
        ]
    }
];