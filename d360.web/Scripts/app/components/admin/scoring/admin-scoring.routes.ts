import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ScoringIndexComponent } from './index.component';
import { ScoringDetailComponent } from './detail.component';

const routes: Routes = [
    { path: '', component: ScoringIndexComponent },
    { path: ':assetTypeUid/:allocationUid', component: ScoringDetailComponent } 
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminScoringRoutingModule { }