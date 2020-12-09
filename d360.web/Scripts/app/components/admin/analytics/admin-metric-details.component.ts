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
import { ScoreTypeAllocation, MetricAssetViewModel, MetricAssetVersionConditionItemViewModel, MetricFieldTypeViewModel, MetricMatchType, MetricAssetVersionConditionItemFieldValueViewModel, MetricGovernanceCheckType, MetricAssetDefinitionGovernanceViewModel, ScoreType } from '../../../models/metrics.model';
import { AdminMetricListComponent } from './admin-metric-list.component';
import { OperatorModel, Operator } from '../../../models/operator.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { RelationshipsService } from '../../../services/relationships.service';

@Component({
    selector: 'd3s-admin-analytics-details',
    templateUrl: 'admin-metric-details.component.html',
    providers: [MetricsService, CompanySettingsService, AssetTypeService, AllocationService, ResponsibilityTypeService, RelationshipsService]
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
    operators: OperatorModel[];

    CheckType = MetricGovernanceCheckType;

    @ViewChild('metricList', { static: false }) metricList: AdminMetricListComponent;
    showConditions: boolean;
    scoreData: any[];
    showDisabled: boolean = false;
    showPassTest: boolean = false;
    responsibilityTypes: any[] = [];
    relationshipTypes: any[] = [];

    constructor(
        secondaryNavService: SecondaryNavService,
        private route: ActivatedRoute,
        private router: Router,
        protected messagesService: MessagesObservableService,
        private metricsService: MetricsService,
        private allocationService: AllocationService,
        private assetTypeService: AssetTypeService,
        private settingsService: CompanySettingsService,
        private responsibilityService: ResponsibilityTypeService,
        private relationshipService: RelationshipsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, secondaryNavService);
        this.areaName = "Scoring Definitions";
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

            this.settingsService.getOperators().subscribe(o => {
                this.operators = o;
            });
            this.responsibilityService.getAdminResponsibilityTypes(this.assetTypeUid).subscribe((data) => {
                if (data && data.length) {
                    this.responsibilityTypes = data;
                }
            });
            this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid).subscribe((data) => {
                if (data && data.length) {
                    this.relationshipTypes = data;
                }
            });
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private changeAssetType(event) {
        this.isLoading = true;
        this.selectedAssetType = event;
        this.areaName = 'Scoring Definitions';
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
            this.formattedScoreCalc = (this.data.isExternallyCalculated ? 'Externally Calculated' : 'Internally Calculated');
        }
    }

    formatConditions() {
        this.conditions.forEach(c => {
            const field = this.metricListFieldTypes.find(f => f.ApiName === c.ConditionFieldTypeName);
            c.OperatorText = this.operators.find(o => o.ID === c.Operator).Name;

            if (field) {
                c.FieldTypeName = field.Name;
                c.FieldType = field;

                switch (field.Type) {
                    case 'Lookup':
                        if (field.Values) {
                            if (field.Values.length > 0) {
                                if (c.Values) {
                                    if (c.Values[0]) {
                                        let valueModel: MetricAssetVersionConditionItemFieldValueViewModel = field.Values.find(o => o.Value === +c.Values[0]);
                                        valueModel = field.Values.find(o => o.Value === +c.Values[0]);
                                        if (valueModel) {
                                            c.SingleValue = c.Values[0];
                                            c.ValuesText = valueModel.Text;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 'Date':
                        if (c.Values) {
                            if (c.Values[0]) {
                                c.SingleValue = c.Values[0];
                                c.ValuesText = new Date(c.Values[0]).toLocaleDateString();
                            }
                        }
                        break;
                    case 'DateTime':
                        if (c.Values) {
                            if (c.Values[0]) {
                                c.SingleValue = c.Values[0];
                                c.ValuesText = new Date(c.Values[0]).toLocaleString();
                            }
                        }
                        break;
                    default:
                        if (c.Values) {
                            if (c.Values[0]) {
                                c.SingleValue = c.Values[0];
                                c.ValuesText = c.Values[0];
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
    private hasPassTest(item: MetricAssetViewModel) {
        if (item && item.Definition && item.Definition.Governance && item.Definition.Governance.Check) {
            return true;
        } else {
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

    selectionChanged(event) {
        this.selectedMetric = event;

        if (this.hasConditions(this.selectedMetric))
            this.showConditions = true;
        else
            this.showConditions = false;

        if (this.hasPassTest(this.selectedMetric) && !this.selectedMetric.IsGroup)
            this.showPassTest = true
        else
            this.showPassTest = false;
    }

    private save() {
        this.formatScoreCalc();
        this.showEdit = false;

        if (this.data.scoreType.toString() == 'DataQuality') {
            this.secondaryNavService.updateObject('firstTabTitle', 'Data Quality Score');
        }

        if (this.data.scoreType.toString() == 'Governance') {
            this.secondaryNavService.updateObject('firstTabTitle', 'Governance Score');
        }

        var needsReroute = this.assetTypeUid != this.data.assetTypeUid;
        if (needsReroute) {
            var url = SiteUrlHelpers.SITE_URL_ADMIN_ROOT + '/' + SiteUrlHelpers.SITE_URL_ADMIN_SCORING + '/' + this.data.assetTypeUid + '/' + this.allocationUid;
            this.router.navigateByUrl(url);
        }
    }

}
