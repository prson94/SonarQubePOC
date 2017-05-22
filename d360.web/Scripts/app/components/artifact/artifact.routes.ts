import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ArtifactComponent } from './artifact.component';
import { ArtifactItemComponent } from './artifact-item.component';
import { ArtifactListComponent } from './artifact-list.component';
import { ArtifactTypeMetricsComponent } from './artifact-type-metrics.component';
import { ArtifactTopLevelListComponent } from './artifact-top-level-list.component';

import { SiteUrlHelpers } from '../../static/site-url-helpers';

const routes: Routes = [
    {
        path: '',
        component: ArtifactComponent,
        children: [
            { path: '', component: ArtifactTopLevelListComponent },
            { path: ':artifactTypeId', component: ArtifactListComponent },
            { path: ':artifactTypeId/:artifactId', component: ArtifactItemComponent },
            { path: 'type/metrics/:artifactTypeId', component: ArtifactTypeMetricsComponent }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ArtifactRoutingModule { }