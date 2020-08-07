import { Input, Component, OnInit, OnDestroy, Output, ChangeDetectorRef, ViewChild } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { AssetTypeMetricModel, AssetTypeApiModel } from '../../../models/asset.model';
import { MetricsService } from '../../../services/metrics.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Router, ActivatedRoute } from '@angular/router';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { AssetService } from '../../../services/asset.service';
import { AssetTypeService } from '../../../services/asset-type.service';
import { SearchResult } from '../../../models/search-result.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AllocationService } from '../../../services/allocations.service';
import { ScoreType, ScoreTypeAllocation, MetricAssetViewModel, MetricAssetVersionConditionViewModel, MetricAssetVersionConditionItemViewModel, MetricFieldTypeViewModel, MetricMatchType, MetricAssetVersionConditionItemFieldValueViewModel } from '../../../models/metrics.model';
import { format } from 'util';
import { AdminMetricListComponent } from './admin-metric-list.component';

@Component({
    selector: 'd3s-admin-analytics-details',
    template: `     <div class="row">
                        <div class="tile tile-detail measures">  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <div *ngIf="!isLoading">
                                <div class="measure-heading">
                                    <div class="title">Score Definition</div>
                                    <div class="actions">
                                        <button igButton icon="fa-pencil" (click)="showEdit = true" tooltip="Edit score definition"></button>
                                    </div>
                                </div>
                                <div class="measure-details">  
                                    <div class="col s6">
                                        <div class="measure-details-item">
                                            <div class="details-header">Asset Type</div>
                                            <div class="details-content">{{data?.assetTypePath}}</div>
                                        </div>
                                        <div class="measure-details-item">
                                            <div class="details-header">Score Calculation</div>
                                            <div class="details-content">{{formattedScoreCalc}}</div>
                                        </div>
                                    </div>
                                    <div class="col s6 measure-ranges">
                                       <div class="measure-details-item">
                                           <div class="details-header">Scoring Bands</div>
                                           <div class="score-ranges">
                                               <div class="score-range">
                                                   <span class="score-box score-poor"></span>
                                                   <span class="text">0%-{{data.lowerThreshold}}%</span>
                                               </div>
                                               <div class="score-range">
                                                   <span class="score-box score-average"></span>
                                                   <span class="text">{{data.lowerThreshold}}.%-{{data.upperThreshold}}%</span>
                                               </div>
                                               <div class="score-range">
                                                   <span class="score-box score-good"></span>
                                                   <span class="text">{{data.upperThreshold}}.%-100%</span>
                                               </div>
                                           </div>
                                       </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="tile tile-detail measures">
                        <div class="measure-heading">
                            <div class="title">Measures</div>
                            <div class="actions">
                                <button igButton icon="fa-plus" (click)="add()" class="ig-button-primary" label="Add Measure"></button>
                            </div>
                        </div>
                        <div class="measure-details">  
                            <div class="col s7">
                                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                                <d3s-admin-metric-list #metricList  
                                            *ngIf="!isLoading"
                                            [metricListFieldTypes]="metricListFieldTypes"
                                            [assetType]="selectedAssetType"
                                            [allocationUid]="allocationUid" 
                                            [scoreType]="data"
                                            [scoreData]="scoreData"
                                            (selectionChange)="selectionChanged($event)"></d3s-admin-metric-list>
                            </div>
                            <div *ngIf="!isLoading && selectedMetric" class="col s5 measure-details-panel">
                                <div>
                                      <div class="panel-heading">
                                          {{selectedMetric?.Name}}
                                      </div>
                                    <div class="measure-details-item">
                                          <div class="details-header">Description</div>
                                          <div class="details-content">{{selectedMetric?.Description ? selectedMetric?.Description :'---'}}</div>
                                      </div>
                                     <div class="measure-details-item">
                                          <div class="details-header">Effective Dates</div>
                                          <div class="details-content">{{selectedMetric?.EffectiveDate | utcDate  | date:'shortDate'}} - Present</div>
                                      </div>
                                     <div *ngIf="!data?.isExternallyCalculated" class="measure-details-item">
                                          <div class="details-header">Weight</div>
                                          <div class="details-content">{{getAsPrecentage(selectedMetric?.Weight)}}</div>
                                      </div>
                                     <div *ngIf="!data?.isExternallyCalculated" class="measure-details-item">
                                          <div class="details-header">Grouping Measure</div>
                                          <div class="details-content">{{selectedMetric?.IsGroup ? 'Yes':'No'}}</div>
                                      </div>
                                      <div *ngIf="showConditions" class="measure-details-item">
                                          <div class="details-header">Asset Conditions</div>
                                          <div class="details-condition" *ngFor="let conditionGroup of selectedMetric?.ConditionGroups">
                                              <div class="condition-content">
                                                  <div class="condition-items">
                                                      <div *ngFor="let condition of conditionGroup.ConditionItems" class="right-space">
                                                        {{condition.FieldTypeName}} {{condition.OperatorText}} {{condition.ValuesText}}
                                                      </div>
                                                  </div>
                                              </div>
                                          </div>
                                      </div>
                                     <div class="measure-details-item">
                                          <div class="details-header">Measure Uid</div>
                                          <div class="details-content">{{selectedMetric?.Uid}}</div>
                                      </div>
                                </div>
                            </div>
                        </div>
                   </div>
                    <d3s-modal *ngIf="data" [title]="editTitle" additionalClasses="medium-dialog" (onClose)="onScoreSaveCancel()" [isVisible]="showEdit">
                        <d3s-admin-allocation-editor [disabled]="data?.hasMeasure" [selection]="data" (onCancel)="showEdit=false;" (onSave)="showEdit=false;"></d3s-admin-allocation-editor>
                    </d3s-modal>
                `,
    providers: [MetricsService, AssetTypeService, AllocationService]
})

export class AdminAnalyticsDetailsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    private selectedAssetType: AssetTypeMetricModel = null;
    private selectedMetric = null;
    routeParamsSubscription: any;
    private data: ScoreTypeAllocation = null;
    private assetTypeUid: string;
    private allocationUid: string;
    formattedScoreCalc: string;
    MatchType: MetricMatchType = MetricMatchType.All;
    private metricListFieldTypes: MetricFieldTypeViewModel[] = [];
    private conditions: MetricAssetVersionConditionItemViewModel[] = [];
    private showEdit: boolean = false;
    private operators = [
        { value: 'eq', label: '=' },
        { value: 'neq', label: '!=' },
        { value: 'lt', label: '<' },
        { value: 'lte', label: '<=' },
        { value: 'gt', label: '>' },
        { value: 'gte', label: '>=' },
    ];

    private maxHeight: number

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
            const field = this.metricListFieldTypes.find(f => f.ID === +c.ConditionFieldTypeID);
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

    onScoreSaveCancel() {
        
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
