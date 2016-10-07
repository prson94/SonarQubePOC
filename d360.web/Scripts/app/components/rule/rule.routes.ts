import * as rule from './index'
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const RuleRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_RULE_ROOT,
        component: rule.RuleComponent,
        children: [
            { path: '', component: rule.RuleListComponent },
            { path: ':ruleId', component: rule.RuleItemComponent }
        ]
    }
];