import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { FusionRulesComponent } from './fusion-rules.component';
import { AdminUserGuard } from '../../../guards/admin-user.guard';


const routes: Routes = [    
    { path: ':fusionId/:fusionTypeId', component: FusionRulesComponent, canActivate: [AdminUserGuard] },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class FusionRuleRoutingModule { }