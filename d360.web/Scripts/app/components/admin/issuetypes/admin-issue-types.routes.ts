import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminIssueTypesComponent } from './admin-issue-types.component';
import { ConfigurationIssueTypeFieldsPageComponent } from './tabs/fields/configuration-issue-type-fields-page.component';

const routes: Routes = [
    { path: '', component: AdminIssueTypesComponent },
	{ path: ':uid/fields', component: ConfigurationIssueTypeFieldsPageComponent },
    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminIssueTypesRoutingModule { }

