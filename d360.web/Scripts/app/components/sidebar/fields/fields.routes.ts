import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FieldDefinitionComponent } from './field-definition.component';

const routes: Routes = [
    { path: '', component: FieldDefinitionComponent },    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class FieldsRoutingModule { }