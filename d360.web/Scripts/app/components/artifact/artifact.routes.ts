import { ArtifactComponent } from './artifact.component';
import { ArtifactItemComponent } from './artifact-item.component';
import { ArtifactListComponent } from './artifact-list.component';
import { ArtifactTopLevelListComponent } from './artifact-top-level-list.component';


import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const ArtifactRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT,
        component: ArtifactComponent,        
        children: [            
            { path: '', component: ArtifactTopLevelListComponent },
            { path: ':artifactTypeId', component: ArtifactListComponent },
            { path: ':artifactTypeId/:artifactId', component: ArtifactItemComponent }                        
        ]
    }
];