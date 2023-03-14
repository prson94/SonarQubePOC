import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReferenceListComponent } from './reference-list.component';
import { ReferenceComponent } from './reference.component';

const routes: Routes = [
    {
		path: 'class/Reference',
        component: ReferenceComponent,
        children: [
			{ path: '', component: ReferenceListComponent },
        ]                
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ReferenceRoutingModule { }