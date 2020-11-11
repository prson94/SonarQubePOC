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
import { ScoreTypeAllocation, MetricAssetViewModel, MetricAssetVersionConditionItemViewModel, MetricFieldTypeViewModel, MetricMatchType, MetricAssetVersionConditionItemFieldValueViewModel, MetricGovernanceCheckType, MetricAssetDefinitionGovernanceViewModel } from '../../../models/metrics.model';
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
    formattedCheck: string = "";

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
            this.formatDefinition();
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

        this.formatDefinition();
    }

    private formatDefinition() {
        if (this.showPassTest && !this.selectedMetric.IsGroup) {
            let gov = <MetricAssetDefinitionGovernanceViewModel>this.selectedMetric.Definition.Governance;
            switch (<any>gov.Check) {
                case 'External':
                    this.formattedCheck = (gov.External.Instructions) ? (' Instruciton string: ' + gov.External.Instructions) : '';
                    break;
                case 'Field':
                    let formattedoperator = this.operators.filter(x => x.ID == gov.Field.Operator).length > 0
                        ? this.operators.filter(x => x.ID == gov.Field.Operator)[0].Name : gov.Field.Operator;
                    let fieldType = this.metricListFieldTypes.filter(x => x.ApiName == gov.Field.FieldTypeName).length > 0
                        ? this.metricListFieldTypes.filter(x => x.ApiName == gov.Field.FieldTypeName)[0] : null;
                    let formattedValue = gov.Field.Values.join(", ");
                    if (fieldType) {
                        if (fieldType.Type == "Lookup") {
                            let fieldValue = +gov.Field.Values[0] ?? -1;

                            let lookupValues = fieldType.Values;
                            formattedValue = lookupValues.filter(x => x.Value == fieldValue).length > 0
                                ? lookupValues.filter(x => x.Value == fieldValue)[0].Text : gov.Field.Values.join(", ");
                        }
                        if (fieldType.Type == "Date" || fieldType.Type == "Date") {
                            let dateValues = [];
                            gov.Field.Values.forEach(x => {
                                let date = new Date(x);
                                dateValues.push(date.toLocaleDateString());
                            });
                            formattedValue = dateValues.join(" and ");
                        }
                        this.formattedCheck = fieldType.Name + " " + formattedoperator + " " + formattedValue;
                    } else {
                        this.formattedCheck = "field not found";
                    }


                    break;
                case 'Owner':
                    let responsibilitytype = this.responsibilityTypes.filter(x => { return x.uid.toLowerCase() == gov.Owner.ResponsibilityTypeUid.toLowerCase() }).length == 1
                        ? this.responsibilityTypes.filter(x => { return x.uid == gov.Owner.ResponsibilityTypeUid })[0] : null;
                    let operatorString = "is assigned";
                    if (gov.Owner.Operator == Operator.NotPopulated || <any>gov.Owner.Operator == "NotPopulated") {
                        operatorString = "is not assigned";
                    }
                    if (responsibilitytype) {
                        this.formattedCheck = responsibilitytype.Name + " " + operatorString;
                    } else {
                        this.formattedCheck = "responsibility type not found";
                    }
                    break;
                case 'Predicate':
                    let predicate = this.relationshipTypes.filter(x => { return x.Predicate.Uid.toLowerCase() == gov.Predicate.PredicateUid.toLowerCase() }).length == 1
                        ? this.relationshipTypes.filter(x => { return x.Predicate.Uid.toLowerCase() == gov.Predicate.PredicateUid.toLowerCase() })[0].Predicate : null;
                    let operatorStringForPredicate = "exists";
                    if (gov.Predicate.Operator == Operator.NotPopulated || <any>gov.Predicate.Operator == "NotPopulated") {
                        operatorStringForPredicate = "does not exist";
                    }
                    this.formattedCheck = predicate.Name + "/" + predicate.Inverse + " " + operatorStringForPredicate; 

                    break;
                case 'Relation':
                    let relationshipType = this.relationshipTypes.filter(x => { return x.Uid.toLowerCase() == gov.Relation.IntersectTypeUid.toLowerCase() }).length == 1
                        ? this.relationshipTypes.filter(x => { return x.Uid.toLowerCase() == gov.Relation.IntersectTypeUid.toLowerCase() })[0] : null;
                    let operatorStringForRelation = "is used";
                    if (gov.Relation.Operator == Operator.NotPopulated || <any>gov.Relation.Operator == "NotPopulated") {
                        operatorStringForRelation = "is not used";
                    }

                    let isSubject = (relationshipType.Subject.Uid.toLowerCase() === this.assetTypeUid.toLowerCase());
                    let isObject = (relationshipType.Object.Uid.toLowerCase() === this.assetTypeUid.toLowerCase());
                    let labelName = "";
                    let assetName = "";
                    let label = "";
                    if (isSubject) {
                        labelName = relationshipType.Predicate.Name;
                        assetName = relationshipType.Subject.Name
                    } else if (isObject) {
                        labelName = relationshipType.Predicate.Inverse;
                        assetName = relationshipType.Object.Name;
                    }
                    label = labelName + " " + assetName;
                    if (relationshipType) {
                        this.formattedCheck = label + " " + operatorStringForRelation;
                    } else {
                        this.formattedCheck = "responsibility type not found";
                    }
                    break;
                default:
                    this.formattedCheck = "";
                    break;

            }
        } else {
            this.formattedCheck = "";
        }
    }
}
