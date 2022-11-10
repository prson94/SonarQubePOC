import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminIssueTypesComponent } from './admin-issue-types.component';

const routes: Routes = [
    { path: '', component: AdminIssueTypesComponent },
    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminIssueTypesRoutingModule { }

