import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ScoreComponent } from './score.component';

const routes: Routes = [
	{ path: ':Uid/score/:scoreType', component: ScoreComponent },
	{ path: ':Uid/score', component: ScoreComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ScoreRoutingModule { }