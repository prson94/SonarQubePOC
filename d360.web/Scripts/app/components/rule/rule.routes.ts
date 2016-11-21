import { RuleComponent } from './rule.component';
import { RuleListComponent } from './rule-list.component';
import { RuleItemComponent } from './rule-item.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const RuleRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_RULE_ROOT,
        component: RuleComponent,
        children: [
            { path: '', component: RuleListComponent },
            { path: ':ruleId', component: RuleItemComponent }
        ]
    }
];