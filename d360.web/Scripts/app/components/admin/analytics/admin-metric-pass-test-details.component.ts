import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core';
import { MetricAssetViewModel, MetricAssetDefinitionGovernanceViewModel, MetricFieldTypeViewModel } from '../../../models/metrics.model';
import { Operator } from '../../../models/operator.model';


@Component({
    selector: 'd3s-admin-metric-pass-test-details',
    templateUrl: 'admin-metric-pass-test-details.component.html',
})

export class AdminMetricPassTestDetailsComponent implements OnChanges, OnDestroy {
    

    @Input() selection: MetricAssetViewModel;
    @Input() operators: any[];
    @Input() metricListFieldTypes: MetricFieldTypeViewModel[] = [];
    @Input() responsibilityTypes: any;
    @Input() relationshipTypes: any;
    @Input() assetTypeUid: any;

    dateVal1: Date;
    dateVal2: Date;
    dateShowType: string;
    showPassTest: boolean;
    formattedCheck: string ="";

    ngOnChanges(changes: SimpleChanges): void {
        if (this.hasPassTest(this.selection) && !this.selection.IsGroup)
            this.showPassTest = true
        else
            this.showPassTest = false;
        this.formatDefinition();
    }

    ngOnInit() {
    }

    ngOnDestroy() {
    }

    private hasPassTest(item: MetricAssetViewModel) {
        if (item && item.Definition && item.Definition.Governance && item.Definition.Governance.Check) {
            return true;
        } else {
            return false;
        }
    }

    private formatDefinition() {
        if (this.showPassTest && !this.selection.IsGroup) {
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
                    if (predicate)
                        this.formattedCheck = predicate.Name + "/" + predicate.Inverse + " " + existsOperatorP;
                    else
                        this.formattedCheck = "";
                    break;
                case 'Relation':
                    let relationshipType = this.relationshipTypes.filter(x => { return x.Uid.toLowerCase() == gov.Relation.IntersectTypeUid.toLowerCase() }).length == 1
                        ? this.relationshipTypes.filter(x => { return x.Uid.toLowerCase() == gov.Relation.IntersectTypeUid.toLowerCase() })[0] : null;
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
}
