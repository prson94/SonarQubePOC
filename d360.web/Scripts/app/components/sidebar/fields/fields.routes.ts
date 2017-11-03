import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { FieldDefinitionComponent } from './field-definition.component';

const routes: Routes = [
    { path: ':objectType/:objectId', component: FieldDefinitionComponent },    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class FieldsRoutingModule { }