import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ResourceMessagesComponent } from './resourcemessages.component';

const routes: Routes = [
    { path: ':resourceID', component: ResourceMessagesComponent},
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ResourceMessagesRoutingModule { }