import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminIssueTypesComponent } from './admin-issue-types.component';
import { ConfigurationIssueTypeAllocationsPageComponent } from './tabs/allocations/configuration-issue-type-allocations-page.component';
import { ConfigurationIssueTypeFieldsPageComponent } from './tabs/fields/configuration-issue-type-fields-page.component';
import { ConfigurationIssueTypeLogPageComponent } from './tabs/log/configuration-issue-type-log-page.component';

const routes: Routes = [
    { path: '', component: AdminIssueTypesComponent },
	{ path: ':uid/fields', component: ConfigurationIssueTypeFieldsPageComponent },
	{ path: ':uid/allocations', component: ConfigurationIssueTypeAllocationsPageComponent },
	{ path: ':uid/log', component: ConfigurationIssueTypeLogPageComponent },
    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminIssueTypesRoutingModule { }

