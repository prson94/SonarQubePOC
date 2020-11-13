import { Input, Component, EventEmitter, Output, OnInit, ViewChild, OnChanges, SimpleChanges, HostListener, ChangeDetectorRef, ViewChildren, QueryList, ChangeDetectionStrategy } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel, MetricAssetVersionConditionViewModel, MetricAssetDefinitionViewModel, MetricAssetDefinitionGovernanceViewModel, MetricAssetDefinitionGovernanceExternalViewModel, MetricUpdateFrequency, Condition, MetricAssetVersionConditionItemViewModel, MetricGovernanceCheckType, MetricAssetDefinitionGovernanceFieldViewModel, MetricAssetDefinitionGovernanceOwnerViewModel, MetricAssetDefinitionGovernanceRelationViewModel, MetricAssetDefinitionGovernancePredicateViewModel } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from "../../../models/form.model";
import { FormHelpers } from '../../../static/form-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { OperatorModel, Operator } from '../../../models/operator.model';
import { FormGroup, FormBuilder, Validators, ValidatorFn, AbstractControl, FormControl } from '@angular/forms';
import { CompanySettingsService } from '../../../services/settings.service';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { FieldTypeHelper } from '../../../models/fieldtype-api.model';
import { FieldTypeAPIModelFieldCondition, FieldCondition } from '../../../models/field-condition-grid.models';
import { FieldConditionGrid } from '../../shared/controls/field-condition-grid/field-condition-grid.component';
import { PropertyGroupComponent } from '../../shared/controls/property-group/property-group.component';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { RelationshipsService } from '../../../services/relationships.service';
import * as _ from 'lodash';


@Component({
    selector: 'd3s-admin-metric-editor',
    templateUrl: './admin-metric-editor.component.html',
    providers: [MetricsService, CompanySettingsService, FieldsObservableService, ResponsibilityTypeService, RelationshipsService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`
    .row-margin{
        margin: 8px 0px;
    }
    .row-label{
        margin: 0px 0px -8px 0px;
    }
    .conditions-row{
        display: flex;
        flex-direction: row;
        width: 100%;
        margin-bottom: 8px;
    }   
    .condition{
        margin-left: 8px;
        flex-shrink: 0;
        flex-grow: 0;
        width: 100%;
        max-width: 150px;
    }
    .condition-med{
        max-width: 308px;
        flex-grow: 1;
    }
    `]

})

export class AdminMetricEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() model: MetricAssetViewModel = null;
    @Input() allocationUid: string;
    @Input() uid: string;
    @Input() assetTypeUid: string;
    @Input() parentUid: string;
    @Input() isExternallyCalculated: boolean;
    @Input() operators: OperatorModel[];
    @Input() scoreData: any;

    @Input() metricEditorFieldTypes: MetricFieldTypeViewModel[] = [];

    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    conditions: FieldCondition[] = [];

    testFieldConditions: FieldCondition[] = [];

    private displayWeight: number;
    private displayEffectiveDate: Date;
    private matchType: string;

    private isLoadingFields: boolean = false;
    private isSaving: boolean = false;
    verb = "Add";
    child = "";

    assetType: any = null;
    metricItem: any = null;
    conditionFormMode = FormMode.Default;
    FormMode = FormMode;
    maxHeight: number = window.innerHeight - 160;
    maxScoreEffectiveDate: Date;
    currentEffectiveDate: Date;
    checkTypeOptions = [];
    metricGovernanceCheckType = MetricGovernanceCheckType;
    updateFrequencyOptions = MetricUpdateFrequency;

    responsibilityTypes: any[] = [];
    responsibilityOperators: any[] = [];
    showMatchPicker: boolean = false;

    relationshipTypes: any[] = [];
    relationshipOperators: any[] = [];

    predicateTypes: any[] = [];
    predicateOperators: any[] = [];

    measurestooltip: string = 'Asset conditions can be used to more specifically target assets of the chosen type to be scored by your measures. '
        + 'Only those assets matching the conditions will be scored using these measures. '
        + 'Where you use multiple conditions, you can specify whether an asset must match all or any of the conditions in order to be score by these measures';

    weightTootlip: string = 'Weight determines the contributions of this measure to the overall score calculated for an asset.'
        + 'For example, a measure with a weight of 50% will contribute twice as much as a measure with a weight of 25%.'
        + 'The sum of all measures used to calculate the score should sum to 100% (at the top-level and within each group of measures).'
        + 'Where they do not, they will be adjusted when used to calculate the score.';

    groupingTooltip: string = 'Grouping measures can be used to organize your measures, collecting together a group of a'
        + 'similar nature(E.g.responsibility assignments, required field checks).'
        + 'Grouping measures do not have asset conditions, as they are not applied directly to the assets.';

    metricForm: FormGroup = null;

    @ViewChildren(PropertyGroupComponent) groups: QueryList<PropertyGroupComponent>;
    private fields: any[] = [];

    delayedReload = _.debounce(() => {
        this.load();
    }, 200);

    constructor(private metricsService: MetricsService,
        protected messagesService: MessagesObservableService,
        private settingsService: CompanySettingsService,
        private fieldsService: FieldsObservableService,
        private responsibilityService: ResponsibilityTypeService,
        private relationshipService: RelationshipsService,
        private fb: FormBuilder,
        private cdRef: ChangeDetectorRef
    ) {
        super();
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
        if (changes['scoreData'] && (changes['scoreData'].currentValue != changes['scoreData'].previousValue)) {
            this.getMaxScoreDate();
        }
        if (changes['assetTypeUid'] && (changes['assetTypeUid'].currentValue != changes['assetTypeUid'].previousValue)) {
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
            matchType: null,
            check: null
        });

        this.checkTypeOptions = Object.keys(this.metricGovernanceCheckType).filter(e => !isNaN(+e)).map(o => {
            let label = MetricGovernanceCheckType[o];
            if (+o == MetricGovernanceCheckType.Relation) {
                label = "Relationship"
            }
            if (+o == MetricGovernanceCheckType.Owner) {
                label = "Ownership"
            }
            return {
                value: +o, label: label
            }
        });

        this.load();
        this.loadFieldData();
    }

    updateFormValidity(event) {
        if (this.groups && this.groups.length > 0) {
            this.groups.forEach(x => { x.refreshBadgeCounts(); });
        }
        this.cdRef.markForCheck();
    }

    loadFieldData() {
        this.isLoadingFields = true;
        this.settingsService.getOperators().subscribe(operators => {
            this.operators = operators;

            this.fieldsService.getFieldsV2(this.assetTypeUid, null, null).subscribe(res => {
                var tempFields: FieldTypeAPIModelFieldCondition[] = [];
                res.forEach(f => {
                    if (FieldTypeHelper.isFieldForOperator(f.Type)) {
                        tempFields.push(f as FieldTypeAPIModelFieldCondition);
                    }
                });

                tempFields.forEach(f => {
                    f.Operators = [];
                    this.operators.forEach(op => {
                        if (op.AllowedDataTypes.some(x => x.Name.toLowerCase() === FieldTypeHelper.getFieldType(f.Type).toLowerCase())) {
                            f.Operators.push({ label: op.Name, value: op.ID });
                        }

                        if (FieldTypeHelper.getFieldType(f.Type) === 'Lookup') {

                            var options = this.metricEditorFieldTypes.find(x => x.ApiName === f.Name);
                            f.Values = [];
                            if (options && options.Values) {
                                options.Values.forEach(val => {
                                    f.Values.push({ value: val.Value.toString(), label: val.Text });
                                })
                            }
                        }

                        if (FieldTypeHelper.getFieldType(f.Type) === 'Boolean') {
                            f.Values = [];
                            f.Values.push({ value: 'true', label: 'True' });
                            f.Values.push({ value: 'false', label: 'False' });
                        }
                    });

                });
                this.fields = tempFields.filter(x => x.Operators.length > 0);
                this.isLoadingFields = false;
                if (this.uid && !this.isExternallyCalculated && !this.model.IsGroup) {
                    if (!this.model.Definition) {
                        this.model.Definition = new MetricAssetDefinitionViewModel();
                        if (!this.isExternallyCalculated) {
                            this.model.Definition.Governance = new MetricAssetDefinitionGovernanceViewModel();
                            this.model.Definition.Governance.Check = null;
                            this.isLoading = false;
                        }
                    } else {
                        this.model.Definition.Governance.Check = MetricGovernanceCheckType[this.model.Definition.Governance.Check + ""];
                        this.loadTestConditions();
                    }
                } else {
                    this.isLoading = false;
                }
                this.cdRef.markForCheck();
            });

            this.responsibilityService.getAdminResponsibilityTypes(this.assetTypeUid).subscribe((data) => {
                if (data && data.length) {
                    this.responsibilityTypes = data.map(x => {
                        return { label: x.Name, value: x.uid };
                    });
                    this.responsibilityOperators = [{ label: "is assigned", value: Operator.Populated }, { label: "is not assigned", value: Operator.NotPopulated }];
                    if (this.model.Definition.Governance && this.model.Definition.Governance.Owner) {
                        this.model.Definition.Governance.Owner.Operator = Operator[this.model.Definition.Governance.Owner.Operator + ""];
                    }
                }
            });
            this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid).subscribe((data) => {
                if (data && data.length) {
                    this.relationshipTypes = data.map(x => {
                        let isSubject = (x.Subject.Uid.toLowerCase() === this.assetTypeUid.toLowerCase());
                        let isObject = (x.Object.Uid.toLowerCase() === this.assetTypeUid.toLowerCase());
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
                    this.predicateTypes = data.map((x, idx, self) => {
                        let isSubject = (x.Subject.Uid.toLowerCase() === this.assetTypeUid.toLowerCase());
                        let hasInverse = self.findIndex((check) => {
                            if (isSubject) {
                                return (check.Subject.Uid.toLowerCase() === x.Object.Uid.toLowerCase() && check.Object.Uid.toLowerCase() === x.Subject.Uid.toLowerCase() && check.Predicate.Uid == x.Predicate.Uid);
                            } else {
                                return (check.Subject.Uid.toLowerCase() === x.Object.Uid.toLowerCase() && check.Object.Uid.toLowerCase() === x.Subject.Uid.toLowerCase() && check.Predicate.Uid == x.Predicate.Uid);
                            }
                        }) != -1;
                        let label = '';
                        if (isSubject) {
                            label = x.Predicate.Name + (hasInverse ? ('/' + x.Predicate.Inverse) : '');
                        } else {
                            label = x.Predicate.Inverse + (hasInverse ? ('/' + x.Predicate.Name) : '');
                        }
                        return { label: label, value: x.Predicate.Uid };
                    });
                    this.predicateTypes = this.predicateTypes.filter((x, pos, self) => (pos == self.findIndex((t) => (t.value == x.value))));
                }
                this.relationshipOperators = [{ label: "is used", value: Operator.Populated }, { label: "is not used", value: Operator.NotPopulated }];
                this.predicateOperators = [{ label: "exists", value: Operator.Populated }, { label: "does not exist", value: Operator.NotPopulated }];
                if (this.model.Definition.Governance && this.model.Definition.Governance.Relation) {
                    this.model.Definition.Governance.Relation.Operator = Operator[this.model.Definition.Governance.Relation.Operator + ""];
                }
                if (this.model.Definition.Governance && this.model.Definition.Governance.Predicate) {
                    this.model.Definition.Governance.Predicate.Operator = Operator[this.model.Definition.Governance.Predicate.Operator + ""];
                }
            });
        })
    }

    load() {
        if (!this.model)
            this.model = new MetricAssetViewModel();
        this.child = "";
        this.model.ParentUid = null;
        this.currentEffectiveDate = null;
        if (this.uid) {
            this.verb = "Edit"
            if (this.model.EffectiveDate !== null) {
                var date = this.utcToLocal(new Date(this.model.EffectiveDate));
                this.currentEffectiveDate = new Date(this.model.EffectiveDate);
                this.displayEffectiveDate = date;
            }
            this.onGroupchange(this.model.IsGroup);
        } else {
            this.model = new MetricAssetViewModel();
            this.model.Weight = null;
            this.model.IsGroup = false;
            this.verb = "Add";
            if (this.parentUid) {
                this.child = "Child";
                this.model.ParentUid = this.parentUid;
            }
            this.model.EffectiveDate = new Date();
            this.model.AllocationUid = this.allocationUid;
            if (!this.model.Definition) {
                this.model.Definition = new MetricAssetDefinitionViewModel();
                if (!this.isExternallyCalculated) {
                    this.model.Definition.Governance = new MetricAssetDefinitionGovernanceViewModel();
                    this.model.Definition.Governance.Check = null;
                }
            }
            this.isLoading = false;
        }
        if (this.model.Weight) {
            this.displayWeight = Math.round(this.model.Weight * 100);
        }

        if (this.model.ConditionGroups && this.model.ConditionGroups.length > 0) {
            if (this.model.ConditionGroups[0].MatchType.toString() === 'All') {
                this.matchType = 'All';
            }
            else this.matchType = 'Any';
        }

        if (!this.model.ConditionGroups || this.model.ConditionGroups.length === 0) {
            const dummyConditionGroup = new MetricAssetVersionConditionViewModel();
            dummyConditionGroup.Position = 1;
            dummyConditionGroup.MatchType = "All";
            this.model.ConditionGroups.push(dummyConditionGroup);
        }

        if (this.model.ConditionGroups && this.model.ConditionGroups.length > 0 && this.model.ConditionGroups[0].ConditionItems) {
            var conditions = this.model.ConditionGroups[0].ConditionItems;
            this.conditions = [];
            if (conditions.length > 0) {
                conditions.forEach(c => {
                    var cond = new FieldCondition();
                    cond['uid'] = c.Uid;
                    cond.field = c.FieldType.ApiName;
                    cond.isValid = true;
                    cond.operator = c.Operator;
                    cond.value = c.Values[0];

                    if (c.FieldType.Type == 'DateTime' || c.FieldType.Type == 'Date') {
                        cond.value = new Date(cond.value);
                    }
                    if (c.FieldType.Type == 'Lookup') {
                        cond.value = cond.value.toString();
                    }


                    this.conditions.push(cond);
                })
            }
        }

        this.getMaxScoreDate();
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
                condition.field = this.model.Definition.Governance.Field.FieldTypeName;
                condition.operator = this.model.Definition.Governance.Field.Operator;
                condition.value = this.model.Definition.Governance.Field.Values[0];
                condition.value2 = this.model.Definition.Governance.Field.Values.length > 1 ? this.model.Definition.Governance.Field.Values[1] : null;

                let field = this.metricEditorFieldTypes.filter(x => x.ApiName == condition.field)[0]

                if (field && (field.Type == "Date" || field.Type == "DateTime")) {
                    let date = new Date(condition.value);
                    condition.value = date;

                    if (condition.value2) {
                        let date = new Date(condition.value2);
                        condition.value2 = date;
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
                break;
            case 3:
                this.metricForm.addControl("PredicateTypeUid", new FormControl(''));
                this.metricForm.addControl("PredicateTypeOperator", new FormControl(''));
                if (!this.model.Definition.Governance.Predicate.Operator) {
                    this.model.Definition.Governance.Predicate.Operator = Operator.Populated;
                }
                break;
            case 4:
                this.metricForm.addControl("IntersectTypeUid", new FormControl(''));
                this.metricForm.addControl("RelationshipTypeOperator", new FormControl(''));
                if (!this.model.Definition.Governance.Relation.Operator) {
                    this.model.Definition.Governance.Relation.Operator = Operator.Populated;
                }
                break;
            default:
                break;
        }
        this.isLoading = false;
    }

    private getMaxScoreDate() {
        if (this.scoreData && this.scoreData.length) {
            let maxDates: any[] = [];
            this.scoreData.forEach(x => {
                if (x.Scores && x.Scores.length > 0) {
                    let scores = x.Scores.sort((x, y) => {
                        let datex = new Date(x.EffectiveDate);
                        let datey = new Date(y.EffectiveDate);
                        return datey.getTime() - datex.getTime();
                    });
                    maxDates.push(new Date(scores[0].EffectiveDate));
                }
            });
            maxDates.sort((x, y) => {
                return y.getTime() - x.getTime();
            });
            this.maxScoreEffectiveDate = maxDates[0];
        }
    }

    validateConditions() {
        let valid = true;
        var toEval = this.conditions.filter(x => x.field);
        if (toEval.length > 1)
            this.showMatchPicker = true;
        else
            this.showMatchPicker = false;
        return true;
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

    onGroupchange(event: boolean) {
        if (this.metricForm) {
            if (this.model.IsGroup) {
                this.metricForm.removeControl("check");
                this.conditions = [];
            } else {
                this.metricForm.addControl("check", new FormControl(''));
            }
        }
        this.cdRef.markForCheck();
    }

    save() {
        this.isSaving = true;
        var prevDate: string | Date = null;
        var previousConditions = [...this.model.ConditionGroups];

        this.model.MatchConditionsOnly = true;


        if (this.displayEffectiveDate !== null) {
            prevDate = this.displayEffectiveDate;
            let d = new Date(this.displayEffectiveDate);
            let condate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0);
            condate.setMinutes(condate.getMinutes() - condate.getTimezoneOffset());
            this.model.EffectiveDate = condate;
        }

        if (!this.isExternallyCalculated) {
            switch (this.model.Definition.Governance.Check) {
                case 0:
                    this.model.Definition.Governance.External.UpdateFrequency = MetricUpdateFrequency.None;
                    break;
                case 1:
                    if (this.testFieldConditions && this.testFieldConditions.length == 1) {
                        let condition = this.testFieldConditions[0];
                        this.model.Definition.Governance.Field.FieldTypeName = condition.field;
                        this.model.Definition.Governance.Field.Operator = condition.operator;
                        let val2 = null
                        if (condition.operator == Operator.Between || <any>condition.operator == "Between")
                            val2 = condition.value2

                        switch (<any>condition.operator) {
                            case Operator.Populated:
                            case Operator.NotPopulated:
                            case Operator.IsTrue:
                            case Operator.IsFalse:
                            case "Populated":
                            case "NotPopulated":
                            case "IsTrue":
                            case "IsFalse":
                                condition.value = null;
                                val2 = null;
                                console.log("works")
                                break;
                        }
                        this.model.Definition.Governance.Field.Values = [condition.value, val2].filter(x => { return x !== null });
                    }
                    break;
                default:
                    break;
            }

            var conditions = this.conditions.filter(x => x.field);

            if (this.matchType == 'Any') {
                this.matchType = 'Any';
            }
            else this.matchType = 'All';

            var arr = this.model.ConditionGroups[0].ConditionItems;
            while (arr.length > 0) {
                arr.pop();
            }

            conditions.forEach(c => {
                var fieldCondition = new MetricAssetVersionConditionItemViewModel();
                fieldCondition.ConditionFieldTypeName = c.field;
                fieldCondition.Operator = c.operator;
                fieldCondition.FieldType = this.metricEditorFieldTypes.filter(x => x.ApiName == c.field)[0];

                if (!fieldCondition.Values) {
                    fieldCondition.Values = [];
                }
                if (fieldCondition.Values.length === 0) {
                    fieldCondition.Values.push('');
                }
                switch (fieldCondition.FieldType.Type) {
                    case 'Date':
                    case 'DateTime':
                        let d = new Date(c.value);
                        let condate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0);
                        condate.setMinutes(condate.getMinutes() - condate.getTimezoneOffset());
                        fieldCondition.Values[0] = condate.toUTCString();
                        break;
                    case 'Lookup':
                        fieldCondition.Values[0] = c.value;
                        break;
                    default:
                        fieldCondition.Values[0] = c.value;
                        break;
                }

                if (c['uid']) {
                    fieldCondition.Uid = c['uid'];
                }
                arr.push(fieldCondition);
            })
        }

        if (!this.isExternallyCalculated) {
            var weight = +this.displayWeight;
            this.model.Weight = +(weight / 100).toFixed(2);
        }

        this.metricsService.saveMetric(this.model)
            .subscribe(r => {
                if (r && r.type != 'error') {
                    this.isSaving = false;
                    this.showMessageForResult(this.messagesService, r);
                    this.onSave.emit(this.model.Name);
                    this.cdRef.markForCheck();
                }
                else {
                    this.displayEffectiveDate = prevDate as Date;
                    this.model.ConditionGroups = [...previousConditions];
                    this.isSaving = false;
                    this.cdRef.markForCheck();
                }
            });
    }

    cancel() {
        this.load();
        this.onCancel.emit(this.model.Name);
        this.model = null;
    }

    getUTCDate(date: Date): Date {
        date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
        return date;
    }
    private utcToLocal(date: Date): Date {
        return new Date(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate(), date.getUTCHours(), date.getUTCMinutes(), date.getUTCSeconds());
    }
    getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }

    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.maxHeight = window.innerHeight - 240;
        this.cdRef.markForCheck();
    }


    isEmptyString(): ValidatorFn {
        type NewType = AbstractControl;

        return (control: NewType): { [key: string]: any } | null => {
            if (control.value == null)
                return {};
            if ((control.value as string).trim() == '' && (control.value as string) != '')
                return {
                    empty: { value: control.value }
                };
            return null;
        };
    }

    isValidWeight(): ValidatorFn {
        type NewType = AbstractControl;
        return (control: NewType): { [key: string]: any } | null => {
            if (control.value == null || control.value == undefined)
                return {};
            if ((control.value as number) < 1 || (control.value as number) > 100)
                return {
                    outOfRange: { value: control.value }
                };
            return null;
        };
    }
};