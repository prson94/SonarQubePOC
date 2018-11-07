import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminExportTemplatesComponent } from './admin-export-templates.component';

const routes: Routes = [
    { path: '', component: AdminExportTemplatesComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminExportTemplatesRoutingModule { }