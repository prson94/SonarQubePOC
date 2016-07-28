import * as rule from './index'

export const RuleRoutes = [
    {
        path: 'a/rule',
        component: rule.RuleComponent,
        children: [
            { path: '', component: rule.RuleListComponent },
            { path: ':ruleId', component: rule.RuleItemComponent }
        ]
    }
];