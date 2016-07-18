import * as diagnostic from './index'

export const DiagnosticRoutes = [
    {
        path: 'a/diagnostic',
        component: diagnostic.DiagnosticComponent,
        children: [
            { path: 'textpath', component: diagnostic.DiagnosticIncorrectTextpathComponent}            
        ]
    }
];