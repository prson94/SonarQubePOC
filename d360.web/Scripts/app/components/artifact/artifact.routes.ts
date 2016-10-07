import * as artifact from './index'
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const ArtifactRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT,
        component: artifact.ArtifactComponent,        
        children: [            
            { path: '', component: artifact.ArtifactTopLevelListComponent },
            { path: ':artifactTypeId', component: artifact.ArtifactListComponent },
            { path: ':artifactTypeId/:artifactId', component: artifact.ArtifactItemComponent }                        
        ]
    }
];