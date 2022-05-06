import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { MetricAssetDefinitionViewModel } from '../../../models/metrics.model';
import { Operator } from '../../../models/operator.model';
import { MetricsService } from '../../../services/metrics.service';
import { CommonScreenReferencesModel } from './common-screen-references-model';
import '@angular/localize/init';

@Component({
    selector: 'd3s-admin-metric-pass-test-details',
    templateUrl: 'admin-metric-pass-test-details.component.html',
})
export class AdminMetricPassTestDetailsComponent implements OnChanges {
    @Input() definition: MetricAssetDefinitionViewModel;
    @Input() assetTypeUid: any;
    @Input() screenReferences: CommonScreenReferencesModel;

    dateVal1: Date;
    dateVal2: Date;
    dateShowType: string;
    showPassTest: boolean;
    formattedCheck: string = "";
    ruleResultFilters: any[];
    ruleResultOperation: string = "";
    ruleResultPathHtml: string = "";

    constructor(protected metricsService: MetricsService) {
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.showPassTest = this.hasPassTest();
        this.formatDefinition();
    }

    private hasPassTest() {
        if (
            this.definition &&
            (this.definition.DataQuality || (this.definition.Governance && this.definition.Governance.Check))
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

    hasRuleResultFilters() {
        let present: boolean = false;
        if (this.showPassTest) {
            if (this.isDataQualityMeasure()) {
                if (this.definition.DataQuality) {
                    present = (this.definition.DataQuality.Filters && this.definition.DataQuality.Filters.length > 0);
                }
            }
        }
        return present;
    }

    private formatDefinition() {
        if (this.showPassTest) {

            if (this.isDataQualityMeasure()) {
                if (this.definition.DataQuality && this.screenReferences.paths) {
                    const dq = this.definition.DataQuality;

                    const resultPathUid = dq.ResultPathUid;

                    this.ruleResultOperation = dq.ResultOperation.toString();

                    const paths = this.screenReferences.paths.filter(x => { return x.value == resultPathUid; });

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
                                            const operatorText = this.screenReferences.operators.find(o => o.ID === f.Operator).Name;
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
                        this.formattedCheck = (gov.External.Instructions) ? ($localize`External: Instruction string` + `: ` + gov.External.Instructions) : $localize`External`;
                        break;
                    case 'Field':
                        let formattedoperator = this.screenReferences.operators.find(x => x.ID == gov.Field.Operator);
                        let fieldType = this.screenReferences.fields.find(x => x.ApiName == gov.Field.FieldTypeName);
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
                            this.formattedCheck = fieldType.Name + " " + formattedoperator.Name + " " + formattedValue;
                        } else {
                            this.formattedCheck = $localize`field not found`;
                        }


                        break;
                    case 'Owner':
                        let responsibilitytype = this.screenReferences.responsibilities.find(x => { return x.uid.toLowerCase() == gov.Owner.ResponsibilityTypeUid.toLowerCase() });
                        let operatorString = $localize`is assigned`;
                        if (gov.Owner.Operator == Operator.NotPopulated || <any>gov.Owner.Operator == "NotPopulated") {
                            operatorString = $localize`is not assigned`;
                        }
                        if (responsibilitytype) {
                            this.formattedCheck = responsibilitytype.Name + " " + operatorString;
                        } else {
                            this.formattedCheck = $localize`responsibility type not found`;
                        }
                        break;
                    case 'Predicate':
                        let predicate = this.screenReferences.predicates.find(x => { return x.Uid.toLowerCase() == gov.Predicate.PredicateUid.toLowerCase() });
                        let existsOperatorP = $localize`exists`;
                        if (gov.Predicate.Operator == Operator.NotPopulated || <any>gov.Predicate.Operator == "NotPopulated") {
                            existsOperatorP = $localize`does not exist`;
                        }
                        if (predicate)
                            this.formattedCheck = predicate.Name + "/" + predicate.Inverse + " " + existsOperatorP;
                        else
                            this.formattedCheck = "";
                        break;
                    case 'Relation':
                        let relationshipType = this.screenReferences.relationships.find(x => { return x.Uid.toLowerCase() == gov.Relation.IntersectTypeUid.toLowerCase() });
                        let existsOperator = $localize`exists`;
                        if (gov.Relation.Operator == Operator.NotPopulated || <any>gov.Relation.Operator == "NotPopulated") {
                            existsOperator = $localize`does not exist`;
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
                            this.formattedCheck = $localize`Relationship not found`;
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


    public getPassTestValue() {
        if (!this.formattedCheck && !this.definition)
            return '';
        var prefix = '';
        let check: string = '';
        if (this.definition && this.definition.Governance)
            check = this.definition.Governance.Check.toString();

        if (!check)
            return '';

        switch (check) {
            case 'External': prefix = ''; break;
            case 'Field': prefix = $localize`Field` + ': '; break;
            case 'Owner': prefix = $localize`Ownership` + ': '; break;
            case 'Predicate': prefix = $localize`Predicate` + ': '; break;
            case 'Relation': prefix = $localize`Relationship` + ': '; break;
            default: ' default';
        }
        return prefix + this.formattedCheck;
    }
}
