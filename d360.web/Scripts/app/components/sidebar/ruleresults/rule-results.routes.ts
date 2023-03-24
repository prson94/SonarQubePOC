import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RuleResultsComponent } from './rule-results.component';

const routes: Routes = [
    { path: '', component: RuleResultsComponent },   
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class RuleResultsRoutingModule { }