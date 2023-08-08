import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AssignmentSidebarComponent } from './assignment-sidebar.component';

const routes: Routes = [
	{ path: '', component: AssignmentSidebarComponent }
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule]
})
export class AssignmentSidebarRoutes {
}
