import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ReferenceListComponent } from './reference-list.component';
import { ReferenceComponent } from './reference.component';

const routes: Routes = [
    {
        path: '',
        component: ReferenceComponent,
        children: [
            { path: ':referenceListId', component: ReferenceListComponent },
            { path: '', component: ReferenceListComponent },
        ]                
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ReferenceRoutingModule { }