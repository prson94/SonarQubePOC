import { Component, OnInit, OnChanges, SimpleChanges, ChangeDetectorRef, ChangeDetectionStrategy, ViewEncapsulation } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetDefinitionViewModel, MetricAssetDefinitionGovernanceViewModel, MetricAssetDefinitionGovernanceExternalViewModel, MetricUpdateFrequency, MetricGovernanceCheckType, MetricAssetDefinitionGovernanceFieldViewModel, MetricAssetDefinitionGovernanceOwnerViewModel, MetricAssetDefinitionGovernanceRelationViewModel, MetricAssetDefinitionGovernancePredicateViewModel } from '../../../models/metrics.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Operator } from '../../../models/operator.model';
import { FormBuilder, Validators, FormControl } from '@angular/forms';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { FieldCondition } from '../../../models/field-condition-grid.models';
import * as _ from 'lodash';
import { BaseMeasureEditorComponent } from './measure-editor-base.component';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'governance-measure-editor',
    templateUrl: './measure-editor-governance.component.html',
    providers: [MetricsService, FieldsObservableService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['measure-editor.less']
})
export class GovernanceMeasureEditorComponent extends BaseMeasureEditorComponent implements OnInit, OnChanges {

    predicateTypes: any[] = [];
    relationshipTypes: any[] = [];
    responsibilityOperators: any[] = [];
    responsibilityTypes: any[] = [];
    testFieldConditions: FieldCondition[] = [];

    //#region Local reference lists

    checkTypeOptions = [
        { label: "Field", value: MetricGovernanceCheckType.Field },
        { label: "Ownership", value: MetricGovernanceCheckType.Owner },
        { label: "Relationship", value: MetricGovernanceCheckType.Relation },
        { label: "Predicate", value: MetricGovernanceCheckType.Predicate },
        { label: "External", value: MetricGovernanceCheckType.External }
    ];
    existsOperators = [
        { label: "exists", value: Operator.Populated },
        { label: "does not exist", value: Operator.NotPopulated }
    ];
    restrictedPredicateTypes = [
        "Diagram",
        "DiagramUse",
        "DiagramReference",
        "InterTypeHierarchy",
        "IntraTypeHierarchy"
    ];
    restrictedTypes = [];
    updateFrequencyOptions: MetricUpdateFrequency[] = [];

    //#endregion

    delayedReload = _.debounce(() => {
        this.load();
        this.loadFieldData();
    }, 200);

    constructor(protected metricsService: MetricsService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        protected fieldsService: FieldsObservableService,
        protected fb: FormBuilder,
        protected cdRef: ChangeDetectorRef
    ) {
        super(fieldsService, metricsService, messagesService, settingsService, cdRef);
    }

    ngOnChanges(changes: SimpleChanges): void {
        let requiredLoad = false;
        if (changes['uid'] && (changes['uid'].currentValue != changes['uid'].previousValue && !changes['uid'].firstChange)) {
            this.isLoading = true;
            requiredLoad = true;
        }
        if (changes['parentUid'] && (changes['parentUid'].currentValue != changes['parentUid'].previousValue && !changes['parentUid'].firstChange)) {
            this.isLoading = true;
            requiredLoad = true;
        }
        if (requiredLoad)
            this.delayedReload();

        this.cdRef.markForCheck();
    }

    ngOnInit() {
        this.metricForm = this.fb.group({
            name: ['', [Validators.required, this.isEmptyString()]],
            description: null,
            effectiveDate: null,
            weight: ['', [this.isValidWeight()]],
            isGroup: null,
            check: null,
            matchType: null,
            MatchConditionsOnly: null
        });

        this.metricForm.updateValueAndValidity();

        this.metricForm.valueChanges.subscribe(() => {
            setTimeout(() => {
                this.checkModelChanged();
            })
        });
        this.load();
        this.loadFieldData();
    }

    ngAfterViewInit() {
        this.originalConditions = _.cloneDeep(this.conditionGroups)
        this.originalModel = _.cloneDeep(this.model);
        this.originalEffectiveDate = new Date(this.displayEffectiveDate?.toString());
        if (!this.uid) {
            this.hasModelChanged = true;
        }
    }

    updateFormValidity(event) {
        if (this.groups && this.groups.length > 0) {
            this.groups.forEach((x) => { x.refreshBadgeCounts(); });
        }
        this.checkModelChanged();
        this.cdRef.markForCheck();
    }

    loadFieldData() {
        this.loadConditionFieldOptions().subscribe((result) => {
            if (this.uid && !this.model.IsGroup) {
                if (this.model.Definition) {
                    this.model.Definition.Governance.Check = MetricGovernanceCheckType[this.model.Definition.Governance.Check + ""];
                    this.loadTestConditions();
                } else {
                    this.model.Definition = new MetricAssetDefinitionViewModel();
                    this.model.Definition.Governance = new MetricAssetDefinitionGovernanceViewModel();
                    this.model.Definition.Governance.Check = null;
                    this.isLoading = false;
                }
            } else {
                this.isLoading = false;
            }
            this.checkModelChanged();
            this.cdRef.markForCheck();
        });

        if (this.screenReferences.responsibilities && this.screenReferences.responsibilities.length) {
            this.responsibilityTypes = this.screenReferences.responsibilities.map((x) => {
                return { label: x.Name, value: x.uid };
            });
            this.responsibilityOperators = [{ label: "is assigned", value: Operator.Populated }, { label: "is not assigned", value: Operator.NotPopulated }];
            if (this.model.Definition.Governance && this.model.Definition.Governance.Owner) {
                this.model.Definition.Governance.Owner.Operator = Operator[this.model.Definition.Governance.Owner.Operator + ""];
            }
        }

        if (this.screenReferences.predicates && this.screenReferences.predicates.length) {
            this.predicateTypes = this.screenReferences.predicates
                .filter(x => this.restrictedPredicateTypes.indexOf(x.Type) == -1)
                .map((x, idx, self) => {
                    let label = x.Name + '/' + x.Inverse + ' (' + x.FriendlyTypeName + ')';
                    return { label: label, value: x.Uid };
                });
            this.predicateTypes = this.predicateTypes
                .filter((x, pos, self) => (pos == self.findIndex((t) => (t.value == x.value))));
        }

        if (this.screenReferences.relationships && this.screenReferences.relationships.length) {
            this.relationshipTypes = this.screenReferences.relationships
                .filter(x => this.restrictedPredicateTypes.indexOf(x.Predicate.Type) == -1)
                .map(x => {
                    let isSubject = (x.Subject.Uid.toLowerCase() === this.allocation.assetTypeUid.toLowerCase());
                    let isObject = (x.Object.Uid.toLowerCase() === this.allocation.assetTypeUid.toLowerCase());
                    let label = "";
                    let assetLabel = "";
                    if (isSubject) {
                        label = x.Predicate.Name;
                        assetLabel = x.Object.Name
                    } else if (isObject) {
                        label = x.Predicate.Inverse;
                        assetLabel = x.Subject.Name
                    }
                    label = label + " " + assetLabel;
                    return { label: label, value: x.Uid };
                });
        }

        if (this.model.Definition.Governance && this.model.Definition.Governance.Relation) {
            this.model.Definition.Governance.Relation.Operator = Operator[this.model.Definition.Governance.Relation.Operator + ""];
        }
        if (this.model.Definition.Governance && this.model.Definition.Governance.Predicate) {
            this.model.Definition.Governance.Predicate.Operator = Operator[this.model.Definition.Governance.Predicate.Operator + ""];
        }

        this.cdRef.markForCheck();
    }

    load() {
        this.setFormPropertiesBasedOnMode();
        if (this.isEditBasedOnUid()) {
            this.onGroupChange(this.model.IsGroup);
        }
        else {
            if (!this.model.Definition) {
                this.model.Definition = new MetricAssetDefinitionViewModel();
                this.model.Definition.Governance = new MetricAssetDefinitionGovernanceViewModel();
                this.model.Definition.Governance.Check = null;
                this.model.MatchConditionsOnly = true;
            }
            this.isLoading = false;
        }

        if (this.model.Weight) {
            this.displayWeight = Math.round(this.model.Weight * 100);
        }

        this.loadConditions();
        this.onResize(null);
    }

    loadTestConditions() {
        switch (this.model.Definition.Governance.Check) {
            case 0:
                this.metricForm.addControl("instructionString", new FormControl(''));
                this.metricForm.addControl("updateFrequency", new FormControl(''));
                if (this.model.Definition.Governance.External.UpdateFrequency) {
                    this.model.Definition.Governance.External.UpdateFrequency = MetricUpdateFrequency[this.model.Definition.Governance.External.UpdateFrequency + ""];
                }
                break;
            case 1:
                let condition = new FieldCondition();
                condition.field = `${this.allocation.assetTypeUid}.${this.model.Definition.Governance.Field.FieldTypeName}`;
                condition.operator = this.model.Definition.Governance.Field.Operator;
                condition.value = this.model.Definition.Governance.Field.Values[0];
                condition.value2 = this.model.Definition.Governance.Field.Values.length > 1 ? this.model.Definition.Governance.Field.Values[1] : null;

                let field = this.screenReferences.fields.filter((x) => x.ApiName === this.model.Definition.Governance.Field.FieldTypeName)[0]

                if (field && (field.Type == "Date" || field.Type == "DateTime")) {
                    let date = new Date(condition.value);
                    var utc = this.getUtcDate(date);
                    condition.value = utc;

                    if (condition.value2) {
                        let date = new Date(condition.value2);
                        var utc = this.getUtcDate(date);
                        condition.value2 = utc;
                    }
                }
                this.testFieldConditions.push(condition);
                this.cdRef.markForCheck();
                break;
            case 2:
                this.metricForm.addControl("ResponsibilityTypeUid", new FormControl(''));
                this.metricForm.addControl("ResponsibilityTypeOperator", new FormControl(''));
                if (!this.model.Definition.Governance.Owner.Operator) {
                    this.model.Definition.Governance.Owner.Operator = Operator.Populated;
                }
                this.cdRef.markForCheck();
                break;
            case 3:
                this.metricForm.addControl("PredicateTypeUid", new FormControl(''));
                this.metricForm.addControl("PredicateTypeOperator", new FormControl(''));
                if (!this.model.Definition.Governance.Predicate.Operator) {
                    this.model.Definition.Governance.Predicate.Operator = Operator.Populated;
                }
                this.cdRef.markForCheck();
                break;
            case 4:
                this.metricForm.addControl("IntersectTypeUid", new FormControl(''));
                this.metricForm.addControl("RelationshipTypeOperator", new FormControl(''));
                if (!this.model.Definition.Governance.Relation.Operator) {
                    this.model.Definition.Governance.Relation.Operator = Operator.Populated;
                }
                this.cdRef.markForCheck();
                break;
            default:
                break;
        }
        this.isLoading = false;
    }

    testTypeChange(event) {
        this.resetGovernanceDefinition();
        switch (this.model.Definition.Governance.Check) {
            case 0:
                this.metricForm.addControl("instructionString", new FormControl(''));
                this.metricForm.addControl("updateFrequency", new FormControl(''));
                this.model.Definition.Governance.External = new MetricAssetDefinitionGovernanceExternalViewModel();
                break;
            case 1:
                this.model.Definition.Governance.Field = new MetricAssetDefinitionGovernanceFieldViewModel();
                break;
            case 2:
                this.metricForm.addControl("ResponsibilityTypeUid", new FormControl(''));
                this.metricForm.addControl("ResponsibilityTypeOperator", new FormControl(''));
                this.model.Definition.Governance.Owner = new MetricAssetDefinitionGovernanceOwnerViewModel();
                this.model.Definition.Governance.Owner.Operator = Operator.Populated;
                break;
            case 3:
                this.metricForm.addControl("PredicateTypeUid", new FormControl(''));
                this.metricForm.addControl("PredicateTypeOperator", new FormControl(''));
                this.model.Definition.Governance.Predicate = new MetricAssetDefinitionGovernancePredicateViewModel();
                this.model.Definition.Governance.Predicate.Operator = Operator.Populated;
                break;
            case 4:
                this.metricForm.addControl("IntersectTypeUid", new FormControl(''));
                this.metricForm.addControl("RelationshipTypeOperator", new FormControl(''));
                this.model.Definition.Governance.Relation = new MetricAssetDefinitionGovernanceRelationViewModel();
                this.model.Definition.Governance.Relation.Operator = Operator.Populated;
                break;
            default:
                break;
        }
        this.cdRef.markForCheck();
    }

    resetGovernanceDefinition() {
        //clear all checks.
        this.model.Definition.Governance.Field = null;
        this.model.Definition.Governance.Owner = null;
        this.model.Definition.Governance.External = null;
        this.model.Definition.Governance.Predicate = null;
        this.model.Definition.Governance.Relation = null;
        this.cdRef.markForCheck();

        //remove the form controls for External
        this.metricForm.removeControl("updateFrequency");
        this.metricForm.removeControl("instructionString");
        //remove the form controls for Owner
        this.metricForm.removeControl("ResponsibilityTypeUid");
        this.metricForm.removeControl("ResponsibilityTypeOperator");
        //remove the form controls for Relation
        this.metricForm.removeControl("IntersectTypeUid");
        this.metricForm.removeControl("RelationshipTypeOperator");

        //clear conditions
        this.testFieldConditions = [];

    }

    onGroupChange(event: boolean) {
        if (this.metricForm) {
            if (this.model.IsGroup) {
                this.metricForm.removeControl("check");
                this.conditionGroups = [];
            } else {
                this.metricForm.addControl("check", new FormControl(''));
                this.loadConditions();
            }
        }
        this.cdRef.markForCheck();
    }

    save() {
        // Specific to Governance measure.
        switch (this.model.Definition.Governance.Check) {
            case 0:
                this.model.Definition.Governance.External.UpdateFrequency = MetricUpdateFrequency.None;
                break;
            case 1:
                if (this.testFieldConditions && this.testFieldConditions.length == 1) {
                    let condition = this.testFieldConditions[0];
                    this.model.Definition.Governance.Field.FieldTypeName = condition.field.split('.')[1]; // {assetTypeUid}.{FieldTypeName}
                    this.model.Definition.Governance.Field.Operator = condition.operator;

                    let val2 = null
                    if (condition.operator == Operator.Between || <any>condition.operator == "Between")
                        val2 = condition.value2

                    if (!this.doesSelectedOperatorAllowValues(<any>condition.operator)) {
                        condition.value = null;
                        val2 = null;
                    }

                    this.model.Definition.Governance.Field.Values = [condition.value, val2].filter(x => { return x !== null });
                }
                break;
            default:
                break;
        }

        this.model.MatchConditionsOnly = (this.matchConditionsOnly === "true");

        // Common
        this.saveMeasure();
    }

    cancel() {
        this.load();
        this.onCancel.emit(this.model.Name);
        this.model = null;
    }

    checkModelChanged() {
        if (!this.model)
            return false;

        if (
            this.model
            && this.originalModel
            && (
                this.model.Name &&
                this.originalModel.Name != this.model.Name
                || this.originalModel.MatchConditionsOnly != this.model.MatchConditionsOnly
                || (this.originalModel.Description && this.originalModel.Description != this.model.Description)
                || (!this.originalModel.Description && !(!this.model.Description || this.model.Description == null || this.model.Description.trim() == ""))
                || (this.displayWeight && (this.originalModel.Weight * 100) != this.displayWeight)
                || (this.displayEffectiveDate && this.getFormattedEffectiveDate(this.originalEffectiveDate).getTime() !== this.getFormattedEffectiveDate(this.displayEffectiveDate).getTime())
                || (!(this.originalModel.IsGroup === this.model.IsGroup))
                || (!(this.originalModel.MatchConditionsOnly === (this.matchConditionsOnly === "true")))
                || this.haveConditionsChanged(this.conditionGroups, this.originalConditions)
                || this.havePassTestCriteriaChanged(this.model.Definition, this.originalModel.Definition)
            )
        ) {
            this.hasModelChanged = true;
        } else {
            this.hasModelChanged = false;
        }

        if (this.verb == "Edit") {
            if (this.hasModelChanged) {
                this.closeLabel = "Discard Changes"
            } else {
                this.closeLabel = "Close"
            }
        }
        this.cdRef.markForCheck();
    }

    havePassTestCriteriaChanged(updated: MetricAssetDefinitionViewModel, original: MetricAssetDefinitionViewModel): boolean {

        if ((updated.Governance && !original.Governance) || (!updated.Governance && original.Governance)) {
            return true;
        }
        if (updated.Governance) {
            if (!(updated.Governance.Check == original.Governance.Check || MetricGovernanceCheckType[updated.Governance.Check] == <any>original.Governance.Check)) {
                return true;
            }

            //Field
            if ((updated.Governance.Field && !original.Governance.Field)
                || (!updated.Governance.Field && original.Governance.Field)) {
                return true;
            }

            if (updated.Governance.Field && this.testFieldConditions[0] &&
                ((this.testFieldConditions[0].field != original.Governance.Field.FieldTypeName)
                    || !(this.testFieldConditions[0].operator == original.Governance.Field.Operator || Operator[this.testFieldConditions[0].operator] == <any>original.Governance.Field.Operator)
                    || original.Governance.Field.Values.length != [this.testFieldConditions[0].value, this.testFieldConditions[0].value2].filter(x => { return x !== null && x !== undefined }).length
                    || (original.Governance.Field.Values.length > 0 && ![this.testFieldConditions[0].value, this.testFieldConditions[0].value2].filter(x => { return x !== null && x !== undefined }).every(v => original.Governance.Field.Values.indexOf(v) > -1))
                )
            ) {
                return true;
            }

            //External
            if (
                (updated.Governance.External && !original.Governance.External)
                || (!updated.Governance.External && original.Governance.External)
            ) {
                return true;
            }

            if (updated.Governance.External &&
                (
                    (updated.Governance.External.Instructions != original.Governance.External.Instructions)
                    || !(updated.Governance.External.UpdateFrequency == original.Governance.External.UpdateFrequency || MetricUpdateFrequency[updated.Governance.External.UpdateFrequency] == <any>original.Governance.External.UpdateFrequency.toString())
                )
            ) {
                return true;
            }

            //Owner/Responsibility
            if ((updated.Governance.Owner && !original.Governance.Owner)
                || (!updated.Governance.Owner && original.Governance.Owner)) {
                return true;
            }
            if (updated.Governance.Owner &&
                (
                    (updated.Governance.Owner.ResponsibilityTypeUid != original.Governance.Owner.ResponsibilityTypeUid)
                    || !(updated.Governance.Owner.Operator == original.Governance.Owner.Operator || Operator[updated.Governance.Owner.Operator] == <any>original.Governance.Owner.Operator)
                )
            ) {
                return true;
            }

            //Predicate
            if ((updated.Governance.Predicate && !original.Governance.Predicate)
                || (!updated.Governance.Predicate && original.Governance.Predicate)) {
                return true;
            }
            if (updated.Governance.Predicate &&
                (
                    (updated.Governance.Predicate.PredicateUid != original.Governance.Predicate.PredicateUid)
                    || !(updated.Governance.Predicate.Operator == original.Governance.Predicate.Operator || Operator[updated.Governance.Predicate.Operator] == <any>original.Governance.Predicate.Operator)
                )
            ) {
                return true;
            }

            //Relation
            if ((updated.Governance.Relation && !original.Governance.Relation)
                || (!updated.Governance.Relation && original.Governance.Relation)) {
                return true;
            }
            if (
                updated.Governance.Relation &&
                (
                    (updated.Governance.Relation.IntersectTypeUid != original.Governance.Relation.IntersectTypeUid)
                    || !(updated.Governance.Relation.Operator == original.Governance.Relation.Operator || Operator[updated.Governance.Relation.Operator] == <any>original.Governance.Relation.Operator)
                    || (
                        updated.Governance.Relation.Values &&
                        !updated.Governance.Relation.Values.every(v => original.Governance.Relation.Values.indexOf(v) > -1)
                    )
                )
            ) {
                return true;
            }
        }

        return false;
    }

};