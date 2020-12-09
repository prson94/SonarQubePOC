import { Component, Input, OnChanges, SimpleChanges, ViewEncapsulation, ViewChild, ElementRef, AfterViewChecked, ChangeDetectorRef } from '@angular/core';
import { forkJoin } from 'rxjs';
import { BaseComponent } from '../../base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { MetricsService } from '../../../../services/metrics.service';
import { ResponsibilityTypeService } from '../../../../services/responsibility-type.service';
import { RelationshipsService } from '../../../../services/relationships.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel, MetricAssetDefinitionGovernanceViewModel } from '../../../../models/metrics.model';
import { OperatorModel, Operator } from '../../../../models/operator.model';

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

    operators: OperatorModel[];
    metricListFieldTypes: MetricFieldTypeViewModel[] = [];
    responsibilityTypes: any[] = [];
    relationshipTypes: any[] = [];

    showPassTest: boolean = true;
    formattedCheck: string = "";
    dateVal1: Date;
    dateVal2: Date;
    dateShowType: string;

    constructor(
        private settingsService: CompanySettingsService,
        private metricsService: MetricsService,
        private responsibilityService: ResponsibilityTypeService,
        private relationshipService: RelationshipsService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes && changes.selectedMetric && changes.assetTypeUid) {
            this.loadData();
        }
        else if (changes && changes.selectedMetric) {
            this.showPassTest = true

            if (this.hasPassTest(this.selectedMetric) && !this.selectedMetric.IsGroup)
                this.showPassTest = true
            else
                this.showPassTest = false;
        }
    }

    loadData() {
        forkJoin(
            this.settingsService.getOperators(),
            this.metricsService.getFieldTypeViewModelsByAssetType(this.assetTypeUid),
            this.responsibilityService.getAdminResponsibilityTypes(this.assetTypeUid),
            this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid)
        ).subscribe(([op, fields, responsibilities, relationships]) => {
            this.operators = op;
            this.metricListFieldTypes = fields;
            if (responsibilities && responsibilities.length) {
                this.responsibilityTypes = responsibilities;
            }
            if (relationships && relationships.length) {
                this.relationshipTypes = relationships;
            }

            if (this.hasPassTest(this.selectedMetric) && !this.selectedMetric.IsGroup)
                this.showPassTest = true
            else
                this.showPassTest = false;

        })

    }

    private hasPassTest(item: MetricAssetViewModel) {
        if (item && item.Definition && item.Definition.Governance && item.Definition.Governance.Check) {
            this.formatDefinition();
            return true;
        } else {
            return false;
        }
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

    @ViewChild('scoreDefinitionPanel', { static: false }) scoreDefinitionPanel: ElementRef;
    ngAfterViewChecked() {
        if (this.scoreDefinitionPanel) {

            var table = this.scoreDefinitionPanel.nativeElement as HTMLElement;
            table.style.maxHeight = (window.innerHeight - this.scoreDefinitionPanel.nativeElement.getBoundingClientRect().top - 64) + 'px';

        }
        this.cdRef.detectChanges();
    }
}
