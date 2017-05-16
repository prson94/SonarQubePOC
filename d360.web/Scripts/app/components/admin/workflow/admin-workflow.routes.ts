import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminWorkflowNewComponent } from './admin-workflow-new.component';

const routes: Routes = [    
    { path: 'new', component: AdminWorkflowNewComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminWorkflowRoutingModule { }

