import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ArtifactComponent } from './artifact.component';
import { ArtifactItemComponent } from './artifact-item.component';
import { ArtifactListComponent } from './artifact-list.component';
import { AssetGridTopLevelListComponent } from '../assets-grid/asset-grid-top-level-list.component';

const routes: Routes = [
    {
        path: '',
        component: ArtifactComponent,
        children: [
            { path: 'assets/:class', component: AssetGridTopLevelListComponent },
            { path: ':artifactTypeId', component: ArtifactListComponent },
            { path: ':artifactTypeId/:artifactId', component: ArtifactItemComponent }            
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ArtifactRoutingModule { }