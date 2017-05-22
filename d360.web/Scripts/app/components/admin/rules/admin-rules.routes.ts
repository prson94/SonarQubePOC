import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminRulesComponent } from './admin-rules.component';
import { AdminRuleDimensionsComponent } from './admin-rule-dimensions.component'

const routes: Routes = [
    { path: '', component: AdminRulesComponent },
    { path: 'dimensions', component: AdminRuleDimensionsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminRulesRoutingModule { }