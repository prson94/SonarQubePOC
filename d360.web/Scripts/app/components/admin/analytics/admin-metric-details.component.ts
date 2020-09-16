import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { MetricsService } from '../../../services/metrics.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Router, ActivatedRoute } from '@angular/router';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { AssetTypeService } from '../../../services/asset-type.service';
import { SearchResult } from '../../../models/search-result.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AllocationService } from '../../../services/allocations.service';
import { ScoreTypeAllocation, MetricAssetViewModel, MetricAssetVersionConditionItemViewModel, MetricFieldTypeViewModel, MetricMatchType, MetricAssetVersionConditionItemFieldValueViewModel } from '../../../models/metrics.model';
import { AdminMetricListComponent } from './admin-metric-list.component';

@Component({
    selector: 'd3s-admin-analytics-details',
    templateUrl: 'admin-metric-details.component.html',
    providers: [MetricsService, AssetTypeService, AllocationService]
})

export class AdminAnalyticsDetailsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    selectedAssetType: AssetTypeMetricModel = null;
    selectedMetric = null;
    routeParamsSubscription: any;
    data: ScoreTypeAllocation = null;
    private assetTypeUid: string;
    private allocationUid: string;
    formattedScoreCalc: string;
    MatchType: MetricMatchType = MetricMatchType.All;
    private metricListFieldTypes: MetricFieldTypeViewModel[] = [];
    private conditions: MetricAssetVersionConditionItemViewModel[] = [];
    showEdit: boolean = false;
    private operators = [
        { value: 'eq', label: '=' },
        { value: 'neq', label: '!=' },
        { value: 'lt', label: '<' },
        { value: 'lte', label: '<=' },
        { value: 'gt', label: '>' },
        { value: 'gte', label: '>=' },
    ];

    @ViewChild('metricList', { static: false }) metricList: AdminMetricListComponent;
    showConditions: boolean;
    scoreData: any[];

    constructor(
        secondaryNavService: SecondaryNavService,
        private route: ActivatedRoute,
        private router: Router,
        protected messagesService: MessagesObservableService,
        private metricsService: MetricsService,
        private allocationService: AllocationService,
        private assetTypeService: AssetTypeService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, secondaryNavService);
        this.areaName = "Scoring";
    }

    ngOnInit() {
        this.routeParamsSubscription = this.route.params.subscribe(params => {
            this.assetTypeUid = params['assetTypeUid'];
            this.allocationUid = params['allocationUid'];
            this.isLoading = true;
            this.assetTypeService.GetAssetTypeByUid(this.assetTypeUid).subscribe(res => {
                this.selectedAssetType = { Class: res.Class.Name, Name: res.Name, Uid: res.uid };
                this.changeAssetType(this.selectedAssetType);
            });
            this.metricsService.getFieldTypeViewModelsByAssetType(this.assetTypeUid)
                .subscribe(f => {
                    this.metricListFieldTypes = f;
                });
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private changeAssetType(event) {
        this.isLoading = true;
        this.selectedAssetType = event;
        this.areaName = 'Scoring';
        this.areaLink = '/admin/scoring';
        this.tabTitle = 'Governance Score';

        this.setCommonItems(true, this.selectedAssetType.Name);  
        this.setCommonSecondaryNavTabs(false);
        this.allocationService.getAllocationsByAssetTypeUid(this.assetTypeUid) 
            .subscribe(r => {
                var crumb = new Breadcrumb(this.selectedAssetType.Name, null, null, 'allocation', 1);
                r.forEach(x => {
                    const url = `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SCORING}/${x.assetTypeUid}/${x.uid}`;
                    const searchRes: SearchResult = new SearchResult();
                    searchRes.Name = x.assetTypePath;
                    searchRes.Url = url;
                    searchRes.Uid = x.assetTypeUid;
                    crumb.preLoadedTypeAhead.push(searchRes);

                    x.icon = 'fa-drivers-license-o';
                });

                this.setScoringSecondaryNavTabs(this.selectedAssetType.Uid, this.allocationUid, r);

                this.headerBreadcrumbService.showBreadcrumb(crumb);
                this.allocationService.getAllocationsByAssetTypeUid(this.assetTypeUid).subscribe(res => {
                    if (res && res.length > 0)
                        var items = res.filter(x => { return x.uid == this.allocationUid });
                    if (items.length > 0) {
                        this.data = items[0];
                        this.formatScoreCalc();
                        this.metricsService.getMetricsScores(this.assetTypeUid, this.data.scoreType)
                            .subscribe(f => {
                                if (f && f.items && f.items.length > 0) {
                                    this.scoreData = f.items;
                                }
                            });
                        this.isLoading = false;
                    }
                });
            });
    }

    private formatScoreCalc() {
        if (this.data) {
            this.formattedScoreCalc = (this.data.isExternallyCalculated ? 'Externally Calculated':'Internally Calculated');
        }
    }

    formatConditions() {
        this.conditions.forEach(c => {
            const field = this.metricListFieldTypes.find(f => f.ApiName === c.ConditionFieldTypeName);
            c.OperatorText = this.operators.find(o => o.value === c.Operator).label;
            c.OperatorText = this.parseOperator(field, c.OperatorText);

            if (field) {
                c.FieldTypeName = field.Name;
                c.FieldType = field;

                switch (field.Type) {
                    case 'Lookup':
                        if (field.Values) {
                            if (field.Values.length > 0) {
                                if (c.Values) {
                                    if (c.Values[0].Value) {
                                        let valueModel: MetricAssetVersionConditionItemFieldValueViewModel = field.Values.find(o => o.Value === +c.Values[0].Value);
                                        valueModel = field.Values.find(o => o.Value === +c.Values[0].Value);
                                        if (valueModel) {
                                            c.SingleValue = c.Values[0].Value;
                                            c.ValuesText = valueModel.Text;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        if (c.Values) {
                            if (c.Values[0].Value) {
                                c.SingleValue = c.Values[0].Value;
                                c.ValuesText = c.Values[0].Value;
                            }
                        }
                        break;
                }
            }
        });
    } 

    private hasConditions(item: MetricAssetViewModel) {
        
        if (item && item.ConditionGroups && item.ConditionGroups.length > 0) {
            this.conditions = item.ConditionGroups[0].ConditionItems;
            if (this.conditions && this.conditions.length > 0) {
                this.formatConditions();
                return true;
            } else
                return false;
        } else {
            this.conditions = [];
            return false;
        }
    }

    add() {
        if (this.metricList) {
            this.metricList.add(false);
        }
    }

    close() {
        if (this.metricList)
            this.metricList.close();
    }

    getAsPrecentage(val: number) {
        if (val == 0)
            return '0%';
        if (!val)
            return;
        if (val == 1)
            return '100%'
        let s = val + '0000';
        s = s.replace('0.', '');
        if (s.length > 6)
            s = (s.substr(0, 2)) + '.' + s[2] + "%";
        else
            s = (s.substr(0, 2)) + "%";
        if (s.startsWith('0'))
            s = s.substr(1, s.length);
        return s;
    }
    parseOperator(field: MetricFieldTypeViewModel, OperatorText: string): string {
        if (field) {
            switch (field.Type) {
                case 'Date':
                    switch (OperatorText) {
                        case '=':
                            return 'is'
                        case '!=':
                            return 'is not'
                        case '<':
                            return 'is before'
                        case '>':
                            return 'is after'
                        case '<=':
                            return 'is on or before'
                        case '>=':
                            return 'is on or after'
                        default:
                            return OperatorText;
                    }
                case 'Text':
                case 'Lookup':
                    switch (OperatorText) {
                        case '=':
                            return 'is'
                        case '!=':
                            return 'is not'
                        default:
                            return OperatorText;
                    }
                case 'Decimal':
                case 'Number':
                    switch (OperatorText) {
                        case '=':
                            return 'is'
                        case '!=':
                            return 'is not'
                        case '<':
                            return 'is before'
                        case '>':
                            return 'is after'
                        case '<=':
                            return 'is on or before'
                        case '>=':
                            return 'is on or after'
                        default:
                            return OperatorText;
                    }
                case 'Boolean':
                    switch (OperatorText) {
                        case '=':
                            return 'is'
                        default:
                            return OperatorText;
                    }
            }
        }
        return '';
    }

    selectionChanged(event) {
        this.selectedMetric = event;
        if (this.hasConditions(this.selectedMetric)) {
            this.showConditions = true;
        }
        else {
            this.showConditions = false;
        }
    }
}
