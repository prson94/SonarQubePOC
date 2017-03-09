import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { RuleComponent } from './rule.component';
import { RuleListComponent } from './rule-list.component';
import { RuleItemComponent } from './rule-item.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

const routes: Routes = [
    {
        path: '',
        component: RuleComponent,
        children: [
            //{ path: '', component: RuleListComponent },
            { path: ':ruleTypeId', component: RuleListComponent },
            { path: ':ruleTypeId/:ruleId', component: RuleItemComponent }
        ]
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class RuleRoutingModule { }

