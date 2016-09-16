import * as artifact from './index'

export const ArtifactRoutes = [
    {
        path: 'a/artifact',
        component: artifact.ArtifactComponent,        
        children: [            
            { path: '', component: artifact.ArtifactTopLevelListComponent },
            { path: ':artifactTypeId', component: artifact.ArtifactListComponent },
            { path: ':artifactTypeId/:artifactId', component: artifact.ArtifactItemComponent }                        
        ]
    }
];