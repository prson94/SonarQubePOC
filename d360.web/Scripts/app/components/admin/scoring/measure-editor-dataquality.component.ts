import { Input, Component, OnInit, OnChanges, SimpleChanges, ChangeDetectorRef, ChangeDetectionStrategy, ViewEncapsulation } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricFieldTypeViewModel, MetricAssetDefinitionViewModel, MetricRuleResultOperation, MetricMatchType, MetricPathOptionViewModel, MetricAssetDefinitionDataQualityViewModel, MetricAssetDefinitionDataQualityFilterViewModel } from '../../../models/metrics.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Operator } from '../../../models/operator.model';
import { FormBuilder, Validators, FormControl } from '@angular/forms';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { FieldType, FieldTypeHelper } from '../../../models/fieldtype-api.model';
import { FieldTypeAPIModelFieldCondition, FieldCondition } from '../../../models/field-condition-grid.models';
import * as _ from 'lodash';
import { SelectItem } from 'primeng/api';
import { BaseMeasureEditorComponent } from './measure-editor-base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import { AppSettingsEnum } from '../../../models/settings.model';

@Component({
    selector: 'dataquality-measure-editor',
    templateUrl: './measure-editor-dataquality.component.html',
    providers: [MetricsService, FieldsObservableService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['measure-editor.less']
})
export class DataQualityMeasureEditorComponent extends BaseMeasureEditorComponent implements OnInit, OnChanges {

    //#region Tooltip data

    helpUri: string = "";

    ruleResultsTooltip: string = 'In order to collect scoring results from rules, you need '
        + 'to define at least one relationship type to associate the asset type you are scoring '
        + 'with rule types, either directly or via relationships to other asset types.';// </br><a href="' + this.helpUri + '" target="help"><i class="fa fa-external-link"></i> Read more about Rule Results.</a>';

    ruleResultFiltersTooltip: string = 'Rule Result Filters allow you to target the scoring results you '
        + 'wish to collect more specifically, by filtering on the fields '
        + 'supplied by rules and any intermediate asset types used to relate rules to your scoring asset type.';// </br> <a href="' + this.helpUri + '" target="help"><i class="fa fa-external-link"></i> Read more about Rule Result Filters.</a>';

    //#endregion

    //#region Local reference lists

    ruleResultOperations: SelectItem[] = [
        { label: 'Average', value: "Average" },
        { label: 'Maximum', value: "Maximum" },
        { label: 'Minimum', value: "Minimum" }
    ];

    //#endregion

    originalRuleResultFilters: FieldCondition[] = [];

    showRuleResultMatchPicker: boolean = false;
    ruleResultFields: FieldTypeAPIModelFieldCondition[] = [];
    ruleResultFilters: FieldCondition[] = [];
    ruleResultFiltersMatchType: string;

    delayedReload = _.debounce(() => {
        this.load();
        this.loadFieldData();
    }, 200);

    constructor(
        protected metricsService: MetricsService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        protected fieldsService: FieldsObservableService,
        protected fb: FormBuilder,
        protected cdRef: ChangeDetectorRef
    ) {
        super(fieldsService, metricsService, messagesService, settingsService, cdRef);
        let helpBaseUri: string = this.settingsService.getAppSetting(AppSettingsEnum.HelpBaseUri);
        this.helpUri = helpBaseUri + "Default.htm#d-admin/scoring-definitions.htm?TocPath=Administration%257C_____4";
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
            ruleResultPath: ['', [Validators.required]],
            ruleResultOperation: ['', [Validators.required]],
            ruleResultMatchType: null,
            matchType: null,
            MatchConditionsOnly: [this.matchConditionsOnly]
        });

        this.metricForm.updateValueAndValidity();
        this.load();
        this.loadFieldData();
    }

    ngAfterViewInit() {
        this.originalConditions = _.cloneDeep(this.conditionGroups);
        this.originalModel = _.cloneDeep(this.model);
        this.originalEffectiveDate = new Date(this.displayEffectiveDate?.toString());
        if (this.uid) {
            this.metricForm?.valueChanges.subscribe(() => {
                setTimeout(() => {
                    this.checkModelChanged();
                })
            });

            this.cdRef.detectChanges();
        } else {
            this.hasModelChanged = true;
        }
    }

    updateFormValidity(event) {
        if (this.groups && this.groups.length > 0) {
            this.groups.forEach(x => { x.refreshBadgeCounts(); });
        }
        this.checkModelChanged();
        this.cdRef.markForCheck();
    }

    loadFieldData() {
        if (this.model.Definition.DataQuality.Filters && this.model.Definition.DataQuality.Filters.length > 0) {
            this.ruleResultFiltersMatchType = (this.model.Definition.DataQuality.FilterMatchType.toString() === 'All') ? 'All' : 'Any';
        }

        this.loadConditionFieldOptions().subscribe(result => {
            this.isLoading = false;
            this.cdRef.markForCheck();
        });

        if (this.model.Definition && this.model.Definition.DataQuality && this.model.Definition.DataQuality.ResultPathUid) {

            this.metricsService
                .getRuleResultPathOptionFields(this.model.Definition.DataQuality.ResultPathUid)
                .subscribe(fields => {
                    this.parseRuleResultFilters(fields);

                    if (!this.model.Definition.DataQuality.Filters) {
                        this.model.Definition.DataQuality.Filters = [];
                    }

                    if (this.model.Definition.DataQuality.Filters && this.model.Definition.DataQuality.Filters.length > 0) {
                        const filters = this.model.Definition.DataQuality.Filters;
                        this.ruleResultFilters = [];
                        if (filters.length > 0) {
                            filters.forEach(f => {
                                const filter = new FieldCondition();
                                filter.field = `${f.AssetTypeUid}.${f.FieldTypeName}`;
                                filter.isValid = true;
                                filter.operator = f.Operator;
                                filter.value = f.Values[0];

                                this.ruleResultFilters.push(filter);
                            });
                            this.originalRuleResultFilters = _.cloneDeep(this.ruleResultFilters); // Only copy to original model once successfully loaded and parsed.
                        }
                    }
                    this.cdRef.markForCheck();
                });
        }

        this.cdRef.markForCheck();
    }

    load() {
        this.setFormPropertiesBasedOnMode();
        if (this.isEditBasedOnUid()) {
            this.onGroupChange(this.model.IsGroup);
            if (this.model) {
                this.matchConditionsOnly = (this.model.MatchConditionsOnly ? "true" : "false");
            }
        }
        else {
            if (!this.model.Definition) {
                this.model.Definition = new MetricAssetDefinitionViewModel();
                this.model.Definition.DataQuality = new MetricAssetDefinitionDataQualityViewModel();
                this.model.MatchConditionsOnly = true;
                //this.model.Definition.DataQuality.ResultOperation = MetricRuleResultOperation.Average;
            }
            this.isLoading = false;
        }

        if (this.model.Weight) {
            this.displayWeight = Math.round(this.model.Weight * 100);
        }

        this.loadConditions();
        this.onResize(null);
    }

    onGroupChange(event: boolean) {
        if (this.metricForm) {
            if (this.model.IsGroup) {
                this.metricForm.removeControl("ruleResultPath");
                this.metricForm.removeControl("ruleResultOperation");
                this.metricForm.removeControl("ruleResultMatchType");
                this.metricForm.removeControl("matchType");
                this.conditionGroups = [];
            } else {
                this.metricForm.addControl("ruleResultPath", new FormControl('', [Validators.required]));
                this.metricForm.addControl("ruleResultOperation", new FormControl('', [Validators.required]));
                this.metricForm.addControl("ruleResultMatchType", new FormControl(''));
                this.metricForm.addControl("matchType", new FormControl(''));
                this.loadConditions();
            }
        }
        this.metricForm.updateValueAndValidity();
        this.cdRef.markForCheck();
    }

    onPathChange(event: any) {
        let ruleResultPathUid = this.model.Definition.DataQuality.ResultPathUid;
        this.metricsService.getRuleResultPathOptionFields(ruleResultPathUid).subscribe(fields => {
            this.parseRuleResultFilters(fields);
            this.cdRef.markForCheck();
        });
    }

    parseRuleResultFilters(fields: MetricFieldTypeViewModel[]) {
        this.ruleResultFilters = [];
        this.ruleResultFields = fields.map(f => {
            let fieldOption: FieldTypeAPIModelFieldCondition = {
                AssetTypeUid: f.AssetTypeUid,
                Category: '',
                FriendlyName: f.AssetTypeName + ' > ' + f.Name,
                Name: f.ApiName,
                Operators: [],
                Type: new FieldType(f.Type),
                Values: []
            };
            this.screenReferences.operators.forEach(op => {
                if (op.AllowedDataTypes.some(x => x.Name.toLowerCase() === f.Type.toLowerCase())) {
                    fieldOption.Operators.push({ label: op.Name, value: op.ID });
                }
            });
            if (f.Values) {
                f.Values.forEach(val => {
                    fieldOption.Values.push({ value: val.Value, label: val.Text });
                })
            }

            return fieldOption;
        });
    }

    save() {
        // Specific to DataQuality measure.
        this.model.Definition.DataQuality.ResultOperation = MetricRuleResultOperation[this.model.Definition.DataQuality.ResultOperation + ''];
        this.model.Definition.DataQuality.FilterMatchType = (this.ruleResultFiltersMatchType == "All") ? MetricMatchType.All : MetricMatchType.Any;

        this.model.Definition.DataQuality.Filters = [];
        this.ruleResultFilters = this.ruleResultFilters.filter(x => x.field && x.operator); // Make sure we have valid items selected here.
        this.ruleResultFilters.forEach(f => {
            let filter = new MetricAssetDefinitionDataQualityFilterViewModel();
            let fieldData = f.field.split('.'); // {assetTypeUid}.{FieldTypeName}
            filter.AssetTypeUid = fieldData[0];
            filter.FieldTypeName = fieldData[1];
            filter.Operator = Operator[f.operator + ''];
            let fieldTypes = this.ruleResultFields.filter(x => x.AssetTypeUid == filter.AssetTypeUid && x.Name == filter.FieldTypeName);

            let fieldDataType = 'Text'; //Default

            if (fieldTypes.length > 0) {
                let fieldType = fieldTypes[0].Type;
                fieldDataType = FieldTypeHelper.getFieldType(fieldType);
            }

            if (!filter.Values) {
                filter.Values = [];
            }
            if (filter.Values.length === 0) {
                filter.Values.push('');
            }
            filter.Values[0] = this.getCorrectedValueForRawByDataType(fieldDataType, f.value);

            if (!this.doesSelectedOperatorAllowValues(<any>f.operator)) {
                filter.Values = [];
            }

            this.model.Definition.DataQuality.Filters.push(filter);
        });

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

        this.hasModelChanged = false; //set default before testing.

        if (this.model && !this.originalModel) {
            this.hasModelChanged = true;
        }

        if (this.model && this.originalModel) {
            if (this.model.Name && this.originalModel.Name != this.model.Name) {
                this.hasModelChanged = true;
            }
            if (this.originalModel.Description && this.originalModel.Description != this.model.Description) {
                this.hasModelChanged = true;
            }
            if (!this.originalModel.Description && !(!this.model.Description || this.model.Description == null || this.model.Description.trim() == "")) {
                this.hasModelChanged = true;
            }
            if (this.displayWeight && (this.originalModel.Weight * 100) != this.displayWeight) {
                this.hasModelChanged = true;
            }
            if (this.displayEffectiveDate && this.getFormattedEffectiveDate(this.originalEffectiveDate).getTime() !== this.getFormattedEffectiveDate(this.displayEffectiveDate).getTime()) {
                this.hasModelChanged = true;
            }
            if (!(this.originalModel.IsGroup === this.model.IsGroup)) {
                this.hasModelChanged = true;
            }
            if (!(this.originalModel.MatchConditionsOnly === (this.matchConditionsOnly === "true"))) {
                this.hasModelChanged = true;
            }
            if (this.haveConditionsChanged(this.conditionGroups, this.originalConditions)) {
                this.hasModelChanged = true;
            }
            if (this.havePassTestCriteriaChanged(this.model.Definition, this.originalModel.Definition)) {
                this.hasModelChanged = true;
            }
            if (this.haveRuleConditionsChanged(this.ruleResultFilters.filter(x => x.field), this.originalRuleResultFilters.filter(x => x.field !== ""))) {
                this.hasModelChanged = true;
            }

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
        if ((updated.DataQuality && !original.DataQuality) || (!updated.DataQuality && original.DataQuality)) {
            return true;
        }

        if (updated.DataQuality) {

            if (!original.DataQuality.Filters) { original.DataQuality.Filters = []; }
            if (!updated.DataQuality.Filters) { updated.DataQuality.Filters = []; }

            if (updated.DataQuality.ResultPathUid != original.DataQuality.ResultPathUid) {
                return true;
            }

            if (updated.DataQuality.ResultOperation != original.DataQuality.ResultOperation) {
                return true;
            }

            if (updated.DataQuality.FilterMatchType != original.DataQuality.FilterMatchType) {
                return true;
            }

            if (updated.DataQuality.Filters.length != original.DataQuality.Filters.length) {
                return true;
            }
        }

        return false;
    }
};
