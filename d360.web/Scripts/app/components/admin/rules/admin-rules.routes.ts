import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminRulesComponent } from './admin-rules.component';

const routes: Routes = [
    { path: '', component: AdminRulesComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminRulesRoutingModule { }