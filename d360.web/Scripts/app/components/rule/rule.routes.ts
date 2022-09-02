import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { RuleComponent } from './rule.component';
import { RuleListComponent } from './rule-list.component';

const routes: Routes = [
    {
        path: '',
        component: RuleComponent,
        children: [                        
            { path: ':ruleTypeId', component: RuleListComponent },
        ]
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class RuleRoutingModule { }

