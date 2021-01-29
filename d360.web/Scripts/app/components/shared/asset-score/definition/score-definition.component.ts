import { Component, Input, OnChanges, SimpleChanges, ViewEncapsulation, ViewChild, ElementRef, AfterViewChecked, ChangeDetectorRef, ɵbypassSanitizationTrustResourceUrl } from '@angular/core';
import { forkJoin } from 'rxjs';
import { BaseComponent } from '../../base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { MetricsService } from '../../../../services/metrics.service';
import { ResponsibilityTypeService } from '../../../../services/responsibility-type.service';
import { RelationshipsService } from '../../../../services/relationships.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel, MetricAssetDefinitionGovernanceViewModel, MetricGovernanceCheckType, MetricAssetVersionConditionItemViewModel, MetricAssetVersionConditionItemFieldValueViewModel } from '../../../../models/metrics.model';
import { OperatorModel, Operator } from '../../../../models/operator.model';
import { CommonScreenReferencesModel } from '../../../admin/scoring/common-screen-references-model';
import { AdminMetricPassTestDetailsComponent } from '../../../admin/scoring/admin-metric-pass-test-details.component';

@Component({
    selector: 'score-definition',
    templateUrl: `score-definition.component.html`,
    styleUrls: ['score-definition.less'],
    encapsulation: ViewEncapsulation.None,
    providers: [CompanySettingsService, MetricsService, ResponsibilityTypeService, RelationshipsService]
})
export class ScoreDefinitionComponent extends BaseComponent implements OnChanges, AfterViewChecked {
    @Input() selectedMetric: MetricAssetViewModel;
    @Input() assetTypeUid: string;
    @Input() allocationUid: string;
    @Input() isExternallyCalculated: boolean = false;

    private screenReferences: CommonScreenReferencesModel;

    operators: OperatorModel[];
    metricListFieldTypes: MetricFieldTypeViewModel[] = [];
    responsibilityTypes: any[] = [];
    relationshipTypes: any[] = [];

    isDataLoaded: boolean = false;

    showConditions: boolean;
    private conditions: MetricAssetVersionConditionItemViewModel[] = [];

    @ViewChild("passTestComponent", { static: false }) passTestRef: AdminMetricPassTestDetailsComponent;

    constructor(
        private settingsService: CompanySettingsService,
        private metricsService: MetricsService,
        private responsibilityService: ResponsibilityTypeService,
        private relationshipService: RelationshipsService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.screenReferences = new CommonScreenReferencesModel();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes && (changes.assetTypeUid || changes.allocationUid) && this.assetTypeUid && this.allocationUid) {
            this.loadData();
        }
        else if (changes && changes.selectedMetric && this.assetTypeUid) {
            if (this.hasConditions(this.selectedMetric))
                this.showConditions = true;
            else
                this.showConditions = false;
        }
    }

    loadData() {
        this.isDataLoaded = false;
        forkJoin(
            this.settingsService.getOperators(),
            this.metricsService.getFieldTypeViewModelsByAssetType(this.assetTypeUid),
            this.responsibilityService.getAdminResponsibilityTypes(this.assetTypeUid),
            this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid),
            this.metricsService.getAllocationByUid(this.allocationUid)
        ).subscribe(([op, fields, responsibilities, relationships, alloc]) => {
            this.operators = op;
            this.metricListFieldTypes = fields;
            if (responsibilities && responsibilities.length) {
                this.responsibilityTypes = responsibilities;
            }
            if (relationships && relationships.length) {
                this.relationshipTypes = relationships;
            }

            this.screenReferences.fields = fields;
            this.screenReferences.operators = op;
            if (relationships) {
                this.screenReferences.relationships = relationships;
                this.screenReferences.predicates = relationships.map(x => {
                    return x.Predicate;
                });
            }
            if (responsibilities) {
                this.screenReferences.responsibilities = responsibilities;
            }

            if (alloc) {
                if (alloc.scoreType.toString() == "DataQuality") {
                    this.metricsService.getRuleResultPathOptions(this.assetTypeUid, alloc.scoreType).subscribe(options => {
                        options.forEach(p => {
                            let processedUids: string[] = [];
                            let html: string = p.Path;
                            p.Segments.forEach(s => {
                                // Keep track of Uids we already replaced the paths for, so we do not mess up the resulting HTML.
                                if (processedUids.findIndex(x => { return x == s.AssetTypeUid }) == -1) {
                                    let segmentPath = s.Path.split('->').join(' > ');
                                    html = html.replace(new RegExp(s.Name, 'g'), `<b title="${segmentPath}">${s.Name}</b>`,);
                                    processedUids.push(s.AssetTypeUid);
                                }
                            });
                            html = html.replace('which', ''); //replaces the first instance.
                            html = html.split(' which').join(', which');
                            p.label = html;
                            p.value = p.Uid;
                        });
                        this.screenReferences.paths = options;
                        this.screenReferences = { ...this.screenReferences };
                    });
                }
                else {
                    this.screenReferences.paths = [];
                    this.screenReferences = { ...this.screenReferences };
                    this.cdRef.markForCheck();
                }
            }


            this.isDataLoaded = true;
        })

    }

    @ViewChild('scoreDefinitionPanel', { static: false }) scoreDefinitionPanel: ElementRef;
    ngAfterViewChecked() {
        if (this.scoreDefinitionPanel) {

            var table = this.scoreDefinitionPanel.nativeElement as HTMLElement;
            table.style.maxHeight = (window.innerHeight - this.scoreDefinitionPanel.nativeElement.getBoundingClientRect().top - 64) + 'px';

        }
        this.cdRef.detectChanges();
    }

    public getPassCheckValue(): string {
        if (!this.passTestRef) return "";
        return this.passTestRef.getPassTestValue();
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
}
