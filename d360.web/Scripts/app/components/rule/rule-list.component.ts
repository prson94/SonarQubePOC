import {Input, Component, EventEmitter, Output, OnInit, OnDestroy} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {BaseComponent} from '../shared/base.component';
import {Title} from '@angular/platform-browser';
import {
    GridDefinition,
    GridColumn,
    GridField,
    GridFilterColumn,
    GridFilterExpression,
    GridRelationshipFilterExpression,
    GridAttributeFilterExpression
} from '../../models/grid-definition.model';
import {HeaderBreadcrumbService} from '../../services/header-breadcrumb.service';
import {RulesService} from '../../services/rules.service';
import {GridDefinitionService} from '../../services/grid-definition.service';
import {HeaderActionsService} from '../../services/header-actions.service';
import {PermissionsService} from '../../services/permissions.service';
import {Breadcrumb} from '../../models/breadcrumb.model';
import {Rule, RuleType, RuleClassification, RuleStatus} from '../../models/rule.model';
import {SiteUrlHelpers} from '../../static/site-url-helpers';
import {StringConstants} from '../../static/string-constants';
import {RightSidebarService} from '../../services/right-sidebar.service';
import * as _ from 'lodash';
import { MessagesObservableService } from '../../services/messages-observable.service';

@Component({
    selector: 'd3s-rule-list',
    providers: [GridDefinitionService, RulesService, PermissionsService],
    templateUrl: './rule-list.component.html'
})

export class RuleListComponent extends BaseComponent implements OnInit, OnDestroy {
    routeParamsSubscription: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    ruleTypeId: number;
    private rules: any[] = [];
    private selected: Rule;
    private ruleType: RuleType;
    private showEditor: boolean = false;
    private showDelete: boolean = false;

    columns: GridColumn[] = [];
    fields: GridField[] = [];
    filtercolumns: GridFilterColumn[] = [];

    theDeleteCallback: Function;

    constructor(private route: ActivatedRoute,
                private router: Router,
                protected rulesService: RulesService,
                protected titleService: Title,
                protected messagesService: MessagesObservableService,
                private gridDefinitionService: GridDefinitionService,
                private headerActionsService: HeaderActionsService,
                protected headerBreadcrumbService: HeaderBreadcrumbService,
                protected permissionsService: PermissionsService,
                rightSidebarService: RightSidebarService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;

        this.theDeleteCallback = this.deleteRule.bind(this);
    }

    get globalFilterFields(): string[] {
        let f = this.columns.map(c => c.datafield);
        f.push('ID');
        f.push('Dimension');
        return f;
    }

    ngOnInit() {
        this.routeParamsSubscription = this.route.params.subscribe(params => {

            this.ruleTypeId = +params['ruleTypeId'];
            this.currentAreaNameSubscription =
                this.headerBreadcrumbService
                    .getAreaName('RuleType', this.ruleTypeId)
                    .subscribe(result => { this.currentAreaName = result });
            this.headerBreadcrumbService.setCurrentObjectInfo('RuleType', this.ruleTypeId);

            this.loadPermissions(this.permissionsService, StringConstants.ObjectRuleType, this.ruleTypeId);

            this.getFieldsDefinition();

            this.isLoading = true;
            this.rulesService.getRuleType(this.ruleTypeId)
                .subscribe(result => {
                    this.isLoading = false;
                    this.ruleType = result;
                    this.setObjectInfo('RuleType', this.ruleType.ID);
                    this.headerBreadcrumbService.getFolderTitle('#Data Quality').then((res) => {
                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : res));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.ruleType.Name, `${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${this.ruleTypeId}`,
                            undefined,
                            'RuleType',
                            this.ruleType.ID,
                            undefined,
                            undefined,
                            true));

                        this.headerBreadcrumbService.getFolderIcon(this.currentAreaName ? this.currentAreaName : res).then(icon => {
                            this.rightSidebarService.setCurrentArea(this.ruleType.Name, icon, 'Rules');
                            this.rightSidebarService.setCurrentObject('RuleType', this.ruleType.ID, this.ruleType.Name, null, true);
                            this.setCommonRightSideBar(false, false, this.ruleType.HasDashboards);
                        });
                        this.rightSidebarService.showHeader(true);
                    });
                    this.loadPermissions(this.permissionsService, StringConstants.ObjectRuleType, this.ruleTypeId);
                    this.loadRules();
                    this.setBrowserTitle(this.titleService, this.ruleType.Name);
                });
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.currentAreaNameSubscription.unsubscribe();
        this.routeParamsSubscription.unsubscribe();
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.ruleTypeId, StringConstants.ObjectRuleType).subscribe(
            result => {
                this.columns = result.Columns.filter(x => x.datafield != 'Name');
                this.filtercolumns = result.FilterColumns;
                this.fields = result.Fields;
            }
        );
    }

    customSort($event: { data: any[], field: any, order: number }) {
        $event.data.sort((data1, data2) => {
            const value1 = data1[$event.field];
            const value2 = data2[$event.field];
            let result = 0;

            if (!value1 && value2)
                result = -1;
            else if (value1 && !value2)
                result = 1;
            else if (!value1 && !value2)
                result = 0;
            else if (typeof value1 === 'string' && typeof value2 === 'string') {
                if (!isNaN(Date.parse(value1)) && !isNaN(Date.parse(value2))) {
                    const date1 = new Date(value1).getTime();
                    const date2 = new Date(value2).getTime();
                    result = (date1 < date2) ? -1 : (date1 > date2) ? 1 : 0;
                } else {
                    result = value1.localeCompare(value2);
                }
            } else
                result = (value1 < value2) ? -1 : (value1 > value2) ? 1 : 0;

            return ($event.order * result);
        });
    }

    private loadRules() {
        this.isLoading = true;
        this.rulesService.getRules(this.ruleTypeId)
            .subscribe(result => {
                this.isLoading = false;
                for (let rule of result) {
                    if (!rule.Dimension) rule.Dimension = ""; //prime grid has issues with null objects make sure we dont have any.
                    rule.StatusName = RuleStatus[rule.Status];
                }
                this.rules = result;

                if (this.rules.length && this.rules.length > 0) this.selected = this.rules[0];
            });
    }

    private showAddRule() {
        this.selected = null;
        this.showEditor = true;
    }

    private saveRule(event) {
        this.rulesService.saveRule(event.item)
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.loadRules();
                    this.headerActionsService.emitFavoritesChange();
                }
                this.showEditor = false;
            });
    }

    private showRule(rule) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('rule', rule.ID, this.ruleTypeId));
    }

    private deleteRule(id: number) {
        this.rulesService.deleteRule(id).subscribe(result => {
            this.showMessageForResult(this.messagesService, result);
            this.showDelete = false;
            this.selected = this.rules.length > 0 ? this.rules[0] : null;
            this.rules = this.rules.filter(x => x.ID != id);
            this.headerActionsService.emitFavoritesChange();
        });
    }

    private columnDimSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.rules = _.sortBy(this.rules, 'Dimension');
        if (event.order == -1) this.rules.reverse();
    }
};
