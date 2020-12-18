import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { MetricFieldTypeViewModel, MetricPathOptionViewModel, MetricAssetDefinitionViewModel } from '../../../models/metrics.model';
import { Operator } from '../../../models/operator.model';
import { MetricsService } from '../../../services/metrics.service';

@Component({
    selector: 'd3s-admin-metric-pass-test-details',
    templateUrl: 'admin-metric-pass-test-details.component.html',
})
export class AdminMetricPassTestDetailsComponent implements OnChanges {
    @Input() definition: MetricAssetDefinitionViewModel;
    @Input() operators: any[];
    @Input() metricListFieldTypes: MetricFieldTypeViewModel[] = [];
    @Input() responsibilityTypes: any;
    @Input() relationshipTypes: any;
    @Input() assetTypeUid: any;
    @Input() paths: MetricPathOptionViewModel[] = [];

    dateVal1: Date;
    dateVal2: Date;
    dateShowType: string;
    showPassTest: boolean;
    formattedCheck: string ="";
    ruleResultFilters: any[];
    ruleResultPathHtml: string = '';

    constructor(protected metricsService: MetricsService) {
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.showPassTest = this.hasPassTest();
        this.formatDefinition();
    }

    private hasPassTest() {
        if (
            this.definition &&
            (this.definition.DataQuality || (this.definition.Governance && this.definition.Governance.Check) )
        ) {
            return true;
        } else {
            return false;
        }
    }

    isDataQualityMeasure() {
        return (this.definition && this.definition.DataQuality);
    }

    isGovernanceMeasure() {
        return (this.definition && this.definition.Governance);
    }

    private formatDefinition() {
        if (this.showPassTest) {

            if (this.isDataQualityMeasure()) {
                if (this.definition.DataQuality && this.paths) {
                    const dq = this.definition.DataQuality;

                    const resultPathUid = dq.ResultPathUid;
                    const paths = this.paths.filter(x => { return x.value == resultPathUid; });

                    if (paths.length > 0) {
                        this.ruleResultPathHtml = paths[0].label;

                        if (dq.Filters &&
                            dq.Filters.length > 0) {
                            this.metricsService
                                .getRuleResultPathOptionFields(resultPathUid)
                                .subscribe(fields => {

                                    this.ruleResultFilters = [];

                                    dq.Filters.forEach(f => {
                                        const matches = fields.filter(o => { return o.AssetTypeUid == f.AssetTypeUid && o.ApiName == f.FieldTypeName; });
                                        if (matches.length > 0) {
                                            const o = matches[0];
                                            const operatorText = this.operators.find(o => o.ID === f.Operator).Name;
                                            const filterModel = {
                                                assetTypeName: o.AssetTypeName,
                                                fieldTypeName: o.Name,
                                                operator: operatorText,
                                                value: ''
                                            };

                                            if (f.Values && f.Values.length > 0) {
                                                if (o.Type == 'Lookup') {
                                                    f.Values.forEach(fv => {
                                                        const theseFilterOptions = o.Values.filter(ov => { return ov.Value == fv });
                                                        if (theseFilterOptions.length > 0) {
                                                            theseFilterOptions.forEach(tfo => {
                                                                filterModel.value += (filterModel.value == '' ? '' : ', ') + tfo.Text;
                                                            });
                                                        }
                                                    });
                                                }
                                                else {
                                                    if (o.Type == 'Date' || o.Type == 'DateTime') {
                                                        f.Values.forEach(fv => {
                                                            filterModel.value += (filterModel.value == '' ? '' : ', ') + new Date(fv).toLocaleDateString();
                                                        })
                                                    }
                                                    else {
                                                        filterModel.value = f.Values.join(', ');
                                                    }
                                                }
                                            }

                                            this.ruleResultFilters.push(filterModel);
                                        }
                                    });
                                });
                        }
                    }
                }
            }
            else if (this.isGovernanceMeasure()) {
                let gov = this.definition.Governance;
                this.dateVal1 = null;
                this.dateVal2 = null;
                this.dateShowType = null;
                switch (<any>gov.Check) {
                    case 'External':
                        this.formattedCheck = (gov.External.Instructions) ? ('External: Instruction string: ' + gov.External.Instructions) : 'External';
                        break;
                    case 'Field':
                        let formattedoperator = this.operators.filter(x => x.ID == gov.Field.Operator).length > 0
                            ? this.operators.filter(x => x.ID == gov.Field.Operator)[0].Name : gov.Field.Operator;
                        let fieldType = this.metricListFieldTypes.filter(x => x.ApiName == gov.Field.FieldTypeName).length > 0
                            ? this.metricListFieldTypes.filter(x => x.ApiName == gov.Field.FieldTypeName)[0] : null;
                        let formattedValue = gov.Field.Values.join(", ");
                        if (fieldType) {
                            if (fieldType.Type == "Lookup") {
                                let fieldValue = gov.Field.Values[0] ?? -1;

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
            }

        } else {
            this.formattedCheck = "";
        }
    }
}
