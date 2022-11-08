import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AuditPageComponent } from './audit-page.component';

const routes: Routes = [
	{ path: ':uid/log', component: AuditPageComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AuditRoutingModule { }