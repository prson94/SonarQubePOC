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
import { ScoreTypeAllocation, MetricAssetViewModel, MetricAssetVersionConditionItemViewModel, MetricFieldTypeViewModel, MetricMatchType, MetricAssetVersionConditionItemFieldValueViewModel, MetricGovernanceCheckType, MetricAssetDefinitionGovernanceViewModel, ScoreType, MetricPathOptionViewModel } from '../../../models/metrics.model';
import { MeasureListComponent } from './measure-list.component';
import { OperatorModel, Operator } from '../../../models/operator.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipType } from '../../../models/relationship.model';
import { ResponsibilityType } from '../../../models/responsibility-type.model';
import { Predicate } from '../../../models/predicate.model';
import { CommonScreenReferencesModel } from './common-screen-references-model';

@Component({
    selector: 'd3s-allocation-detail',
    templateUrl: 'detail.component.html',
    providers: [MetricsService, CompanySettingsService, AssetTypeService, AllocationService, ResponsibilityTypeService, RelationshipsService]
})

export class ScoringDetailComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    selectedAssetType: AssetTypeMetricModel = null;
    selectedMetric: MetricAssetViewModel = null;
    selectedRuleResultPath: MetricPathOptionViewModel = null;
    routeParamsSubscription: any;
    allocation: ScoreTypeAllocation = null;
    private allocationUid: string;
    private assetTypeUid: string;
    formattedScoreCalc: string;
    MatchType: MetricMatchType = MetricMatchType.All;
    
    private conditions: MetricAssetVersionConditionItemViewModel[] = [];
    showEdit: boolean = false;
    formattedCheck: string = "";

    CheckType = MetricGovernanceCheckType;

    screenReferences: CommonScreenReferencesModel = new CommonScreenReferencesModel();


    @ViewChild('metricList', { static: false }) metricList: MeasureListComponent;
    showConditions: boolean;

    maxScoreEffectiveDate: Date;
    showDisabled: boolean = false;
    showPassTest: boolean = false;
    

    isMeasureListCommandBarDisabled: boolean = false;

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
            this.allocationUid = params['allocationUid'];
            this.assetTypeUid = params['assetTypeUid'];

            this.isLoading = true;

            this.metricsService.getAllocationByUid(this.allocationUid).subscribe(res => {
                this.allocation = res;

                if (res.scoreType.toString() == "DataQuality") {
                    this.metricsService.getRuleResultPathOptions(this.assetTypeUid, res.scoreType).subscribe(options => {
                        options.forEach(p => {
                            let html: string = p.Path;
                            p.Segments.forEach(s => {
                                let segmentPath = s.Path.split('->').join(' > ');
                                html = html.replace(s.Name, `<b title="${segmentPath}">${s.Name}</b>`);
                            });
                            html = html.replace('which', ''); //replaces the first instance.
                            html = html.split(' which').join(', which');
                            p.label = html;
                            p.value = p.Uid;
                        });
                        this.screenReferences.paths = options;

                        this.isMeasureListCommandBarDisabled = !this.allocation.isExternallyCalculated && options.length == 0;
                    });
                }
                else {
                    this.isMeasureListCommandBarDisabled = false;
                }
            });

            this.assetTypeService.GetAssetTypeByUid(this.assetTypeUid).subscribe(res => {
                this.selectedAssetType = { Class: res.Class.Name, Name: res.Name, Uid: res.uid };
                this.changeAssetType(this.selectedAssetType);
            });

            this.metricsService.getFieldTypeViewModelsByAssetType(this.assetTypeUid).subscribe(f => {
                //this.fields = f;
                this.screenReferences.fields = f;
            });

            this.settingsService.getOperators().subscribe(o => {
                this.screenReferences.operators = o;
            });

            this.responsibilityService.getAdminResponsibilityTypes(this.assetTypeUid).subscribe((data) => {
                if (data && data.length) {
                    this.screenReferences.responsibilities = data;
                }
            });

            this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid).subscribe((data) => {
                if (data && data.length) {

                    this.screenReferences.relationships = data;
                    this.screenReferences.predicates = data.map(x => {
                        return x.Predicate;
                    });
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

                this.setScoringSecondaryNavTabs(this.selectedAssetType.Uid, this.allocation.uid, r);

                this.headerBreadcrumbService.showBreadcrumb(crumb);
                this.allocationService.getAllocationsByAssetTypeUid(this.assetTypeUid).subscribe(res => {
                    if (res && res.length > 0) {
                        const items = res.filter(x => { return x.uid == this.allocation.uid });

                        if (items.length > 0) {
                            this.allocation = items[0];
                            this.formatScoreCalc();
                            this.metricsService.getMetricsScores(this.assetTypeUid, this.allocation.scoreType)
                                .subscribe(f => {
                                    if (f && f.items && f.items.length > 0) {
                                        let maxDates: any[] = [];
                                        f.items.forEach(x => {
                                            if (x.Scores && x.Scores.length > 0) {
                                                let scores = x.Scores.sort((x, y) => {
                                                    let datex = new Date(x.EffectiveDate);
                                                    let datey = new Date(y.EffectiveDate);
                                                    return datey.getTime() - datex.getTime();
                                                });
                                                maxDates.push(new Date(scores[0].EffectiveDate));
                                            }
                                        });
                                        maxDates.sort((x, y) => {
                                            return y.getTime() - x.getTime();
                                        });
                                        this.maxScoreEffectiveDate = maxDates[0];
                                    }
                                });
                            this.isLoading = false;
                        }
                    }
                });
            });
    }

    private formatScoreCalc() {
        if (this.allocation) {
            this.formattedScoreCalc = (this.allocation.isExternallyCalculated ? 'Externally Calculated' : 'Internally Calculated');
        }
    }

    formatConditions() {
        this.conditions.forEach(c => {
            const field = this.screenReferences.fields.find(f => f.ApiName === c.ConditionFieldTypeName);
            c.OperatorText = this.screenReferences.operators.find(o => o.ID === c.Operator).Name;

            if (field) {
                c.FieldTypeName = field.Name;
                c.FieldType = field;

                switch (field.Type) {
                    case 'Lookup':
                        if (field.Values) {
                            if (field.Values.length > 0) {
                                if (c.Values) {
                                    if (c.Values[0]) {
                                        let valueModel: MetricAssetVersionConditionItemFieldValueViewModel = field.Values.find(o => o.Value === c.Values[0]);
                                        valueModel = field.Values.find(o => o.Value === c.Values[0]);
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
        if (
            item &&
            item.Definition &&
            (item.Definition.DataQuality || (item.Definition.Governance && item.Definition.Governance.Check))
        )
        {
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

    selectionChanged(event:MetricAssetViewModel) {
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

        if (this.allocation.scoreType.toString() == 'DataQuality') {
            this.secondaryNavService.updateObject('firstTabTitle', 'Data Quality Score');
        }

        if (this.allocation.scoreType.toString() == 'Governance') {
            this.secondaryNavService.updateObject('firstTabTitle', 'Governance Score');
        }

        var needsReroute = this.assetTypeUid != this.allocation.assetTypeUid;
        if (needsReroute) {
            var url = SiteUrlHelpers.SITE_URL_ADMIN_ROOT + '/' + SiteUrlHelpers.SITE_URL_ADMIN_SCORING + '/' + this.allocation.assetTypeUid + '/' + this.allocation.uid;
            this.router.navigateByUrl(url);
        }
    }

}
