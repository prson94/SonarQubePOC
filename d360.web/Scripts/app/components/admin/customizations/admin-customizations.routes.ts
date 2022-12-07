import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminCustomizationsComponent } from './admin-customizations.component';

const routes: Routes = [
    { path: '', component: AdminCustomizationsComponent }    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminCustomizationsRoutingModule { }