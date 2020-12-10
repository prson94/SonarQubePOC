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

@Component({
    selector: 'd3s-allocation-detail',
    templateUrl: 'detail.component.html',
    providers: [MetricsService, CompanySettingsService, AssetTypeService, AllocationService, ResponsibilityTypeService, RelationshipsService]
})

export class ScoringDetailComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    selectedAssetType: AssetTypeMetricModel = null;
    selectedMetric = null;
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

    fields: MetricFieldTypeViewModel[] = [];
    operators: OperatorModel[];
    predicates: Predicate[];
    relationships: RelationshipType[];
    responsibilities: ResponsibilityType[];

    @ViewChild('metricList', { static: false }) metricList: MeasureListComponent;
    showConditions: boolean;
    scoreData: any[];
    showDisabled: boolean = false;
    showPassTest: boolean = false;
    ruleResultPaths: MetricPathOptionViewModel[] = [];
    dateVal1: Date;
    dateVal2: Date;
    dateShowType: string;

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
                        this.ruleResultPaths = options;
                    });
                }
            });

            this.assetTypeService.GetAssetTypeByUid(this.assetTypeUid).subscribe(res => {
                this.selectedAssetType = { Class: res.Class.Name, Name: res.Name, Uid: res.uid };
                this.changeAssetType(this.selectedAssetType);
            });

            this.metricsService.getFieldTypeViewModelsByAssetType(this.assetTypeUid).subscribe(f => {
                this.fields = f;
            });

            this.settingsService.getOperators().subscribe(o => {
                this.operators = o;
            });

            this.responsibilityService.getAdminResponsibilityTypes(this.assetTypeUid).subscribe((data) => {
                if (data && data.length) {
                    this.responsibilities = data;
                }
            });

            this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid).subscribe((data) => {
                if (data && data.length) {

                    this.relationships = data;
                    this.predicates = data.map(x => {
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
                    if (res && res.length > 0)
                        var items = res.filter(x => { return x.uid == this.allocation.uid });
                    if (items.length > 0) {
                        this.allocation = items[0];
                        this.formatScoreCalc();
                        this.metricsService.getMetricsScores(this.assetTypeUid, this.allocation.scoreType)
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
        if (this.allocation) {
            this.formattedScoreCalc = (this.allocation.isExternallyCalculated ? 'Externally Calculated' : 'Internally Calculated');
        }
    }

    formatConditions() {
        this.conditions.forEach(c => {
            const field = this.fields.find(f => f.ApiName === c.ConditionFieldTypeName);
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
            this.dateVal1 = null;
            this.dateVal2 = null;
            this.dateShowType = null;
            switch (<any>gov.Check) {
                case 'External':
                    this.formattedCheck = (gov.External.Instructions) ? (' Instruction string: ' + gov.External.Instructions) : '';
                    break;
                case 'Field':
                    let formattedoperator = this.operators.filter(x => x.ID == gov.Field.Operator).length > 0
                        ? this.operators.filter(x => x.ID == gov.Field.Operator)[0].Name : gov.Field.Operator;
                    let fieldType = this.fields.filter(x => x.ApiName == gov.Field.FieldTypeName).length > 0
                        ? this.fields.filter(x => x.ApiName == gov.Field.FieldTypeName)[0] : null;
                    let formattedValue = gov.Field.Values.join(", ");
                    if (fieldType) {
                        if (fieldType.Type == "Lookup") {
                            let fieldValue = gov.Field.Values[0] ?? "";

                            let lookupValues = fieldType.Values;
                            formattedValue = lookupValues.filter(x => x.Value == fieldValue).length > 0
                                ? lookupValues.filter(x => x.Value == fieldValue)[0].Text : gov.Field.Values.join(", ");
                        }
                        if (fieldType.Type == "Date") {
                            this.dateShowType = fieldType.Type;
                            this.dateVal1 = gov.Field.Values.length > 0 ? new Date(gov.Field.Values[0]) : null;
                            this.dateVal2 = gov.Field.Values.length > 1 ? new Date(gov.Field.Values[1]) : null;
                            formattedValue = "";

                        }
                        this.formattedCheck = fieldType.Name + " " + formattedoperator + " " + formattedValue;
                    } else {
                        this.formattedCheck = "field not found";
                    }


                    break;
                case 'Owner':
                    let responsibilitytype = this.responsibilities.filter(x => { return x.uid.toLowerCase() == gov.Owner.ResponsibilityTypeUid.toLowerCase() }).length == 1
                        ? this.responsibilities.filter(x => { return x.uid == gov.Owner.ResponsibilityTypeUid })[0] : null;
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
                    let predicate = this.relationships.filter(x => { return x.Predicate.Uid.toLowerCase() == gov.Predicate.PredicateUid.toLowerCase() }).length > 0
                        ? this.relationships.filter(x => { return x.Predicate.Uid.toLowerCase() == gov.Predicate.PredicateUid.toLowerCase() })[0].Predicate : null;
                    let existsOperatorP = "exists";
                    if (gov.Predicate.Operator == Operator.NotPopulated || <any>gov.Predicate.Operator == "NotPopulated") {
                        existsOperatorP = "does not exist";
                    }
                    if (predicate)
                        this.formattedCheck = predicate.Name + "/" + predicate.Inverse + " " + existsOperatorP;
                    else
                        this.formattedCheck = "";
                    break;
                case 'Relation':
                    let relationshipType = this.relationships.filter(x => { return x.Uid.toLowerCase() == gov.Relation.IntersectTypeUid.toLowerCase() }).length == 1
                        ? this.relationships.filter(x => { return x.Uid.toLowerCase() == gov.Relation.IntersectTypeUid.toLowerCase() })[0] : null;
                    let existsOperator = "exists";
                    if (gov.Relation.Operator == Operator.NotPopulated || <any>gov.Relation.Operator == "NotPopulated") {
                        existsOperator = "does not exist";
                    }
                
                    if (relationshipType) {
                        let isSubject = (relationshipType.Subject.Uid.toLowerCase() === this.assetTypeUid.toLowerCase());
                        let isObject = (relationshipType.Object.Uid.toLowerCase() === this.assetTypeUid.toLowerCase());
                        let labelName = "";
                        let assetName = "";
                        let label = "";
                        if (isSubject) {
                            labelName = relationshipType.Predicate.Name;
                            assetName = relationshipType.Object.Name
                        } else if (isObject) {
                            labelName = relationshipType.Predicate.Inverse;
                            assetName = relationshipType.Subject.Name;
                        }
                        label = labelName + " " + assetName;
                        this.formattedCheck = label + " " + existsOperator;
                    } else {
                        this.formattedCheck = "Relationship not found";
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
