import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { SemanticTypeListComponent } from './semantic-type-list.component';
import { SemanticDefinitionComponent } from './semantic-type-definition.component'
import { SemanticsComponent } from './semantics.component';
import { SemanticTypeAssetListComponent } from './semantic-asset-list.component';

const routes: Routes = [
    {
        path: '',
        component: SemanticsComponent,
        children: [
            { path: '', component: SemanticTypeListComponent},
            { path: ':semanticTypeUid', component: SemanticDefinitionComponent },
            { path: ':semanticTypeUid/assets', component: SemanticTypeAssetListComponent }
        ]        
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class SemanticsRoutingModule { }