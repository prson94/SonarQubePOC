import { Component, Input, OnInit, OnDestroy, EventEmitter, Output } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricAssetVersionConditionItemViewModel, MetricAssetVersionConditionItemFieldValueViewModel, MetricFieldTypeViewModel, MetricAssetHistoryViewModel, MetricAssetDefinitionGovernanceViewModel, MetricAssetViewModel } from '../../../models/metrics.model';
import { MetricsService } from '../../../services/metrics.service';
import { TreeNode } from 'primeng/api';
import { OperatorModel, Operator } from '../../../models/operator.model';
import { AssetType, AssetTypeMetricModel } from '../../../models/asset.model';

@Component({
    selector: 'measure-history',
    templateUrl: `./measure-history.component.html`,
    providers: [MetricsService]
})

export class AdminMeasureHistoryComponent extends BaseComponent implements OnInit, OnDestroy {

    @Input() Measure: MetricAssetViewModel;
    @Input() AssetType: AssetTypeMetricModel;
    @Input() assetTypeFields: MetricFieldTypeViewModel[] = [];
    @Input() isExternallyCalculated: boolean = false;
    @Input() operators: OperatorModel[];    
    @Input() responsibilityTypes: any[] = [];
    @Input() relationshipTypes: any[] = [];

    @Output() onClose = new EventEmitter;
    
    private conditions: MetricAssetVersionConditionItemViewModel[] = [];
    private metricHistoryRecords: MetricAssetHistoryViewModel[] = [];
    private metricTree: TreeNode[] = [];
    private selectedNode: TreeNode;
    private selection: MetricAssetHistoryViewModel;
    private showConditions: boolean;

    showPassTest: boolean;
    formattedCheck: string;

    dateVal1: Date;
    dateVal2: Date;
    dateShowType: string;

    constructor(
        private metricsService: MetricsService
    ) {
        super();
    }

    ngOnDestroy(): void {
        this.cancel();
    }

    ngOnInit() {
        this.isLoading = true;
        if (this.Measure.Uid) {
            this.metricsService.getMetricsVersionHistory(this.Measure.Uid)
                .subscribe(result => {
                    this.metricHistoryRecords = result;
                    if (this.metricHistoryRecords) {
                        this.metricHistoryRecords.forEach(g => {
                            let n = {
                                data: g,
                                children: [],
                                expanded: true
                            }

                            this.metricTree.push(n);

                        });
                        if (this.metricTree !== null && this.metricTree.length > 0) {
                            this.selectNode(this.metricTree[0]);
                        }
                    }
                    this.isLoading = false;
                });
        }
        else {
            this.selection = null;
            this.metricTree = [];
        }
    }

    cancel() {
        this.onClose.emit(null);
    }

    formatConditions() {
        this.conditions.forEach(c => {
            const field = this.assetTypeFields.find(f => f.ApiName === c.ConditionFieldTypeName);
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

    private hasConditions(item: MetricAssetHistoryViewModel) {
        if (item && item.ConditionGroups && item.ConditionGroups.length > 0) {
            this.conditions = item.ConditionGroups[0].ConditionItems;
            this.formatConditions();
            return true;
        } else {
            this.conditions = [];
        }
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

    public selectNode(e: any) {
        if (e == null)
            return;
        this.selectedNode = e;
        this.selection = e === null ? null : e.data;

        if (this.hasConditions(this.selection)) {
            this.showConditions = true;
        }
        else {
            this.showConditions = false;
        }

        if (this.hasPassTest(this.selection) && !this.Measure.IsGroup)
            this.showPassTest = true
        else
            this.showPassTest = false;

        this.formatDefinition();
    }

    private hasPassTest(item: MetricAssetHistoryViewModel) {
        if (item && item.Definition && item.Definition.Governance && item.Definition.Governance.Check) {            
            return true;
        } else {
            return false;
        }
    }

    private formatDefinition() {
        if (this.showPassTest && !this.Measure.IsGroup) {
            let gov = <MetricAssetDefinitionGovernanceViewModel>this.selection.Definition.Governance;
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
                    let fieldType = this.assetTypeFields.filter(x => x.ApiName == gov.Field.FieldTypeName).length > 0
                        ? this.assetTypeFields.filter(x => x.ApiName == gov.Field.FieldTypeName)[0] : null;
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
                    let predicate = this.relationshipTypes.filter(x => { return x.Predicate.Uid.toLowerCase() == gov.Predicate.PredicateUid.toLowerCase() }).length > 0
                        ? this.relationshipTypes.filter(x => { return x.Predicate.Uid.toLowerCase() == gov.Predicate.PredicateUid.toLowerCase() })[0].Predicate : null;
                    let existsOperatorP = "exists";
                    if (gov.Predicate.Operator == Operator.NotPopulated || <any>gov.Predicate.Operator == "NotPopulated") {
                        existsOperatorP = "does not exist";
                    }
                    if (predicate) {
                        this.formattedCheck = predicate.Name + "/" + predicate.Inverse + " " + existsOperatorP;
                    } else {
                        this.formattedCheck = "";
                    }                    
                    break;
                case 'Relation':
                    let relationshipType = this.relationshipTypes.filter(x => { return x.Uid.toLowerCase() == gov.Relation.IntersectTypeUid.toLowerCase() }).length == 1
                        ? this.relationshipTypes.filter(x => { return x.Uid.toLowerCase() == gov.Relation.IntersectTypeUid.toLowerCase() })[0] : null;
                    let existsOperator = "exists";
                    if (gov.Relation.Operator == Operator.NotPopulated || <any>gov.Relation.Operator == "NotPopulated") {
                        existsOperator = "does not exist";
                    }

                    let isSubject = (relationshipType.Subject.Uid.toLowerCase() === this.AssetType.Uid.toLowerCase());
                    let isObject = (relationshipType.Object.Uid.toLowerCase() === this.AssetType.Uid.toLowerCase());
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
                    if (relationshipType) {
                        this.formattedCheck = label + " " + existsOperator;
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