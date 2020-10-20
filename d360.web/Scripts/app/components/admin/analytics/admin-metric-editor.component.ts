import { Input, Component, EventEmitter, Output, OnInit, ViewChild, ElementRef, OnChanges, SimpleChanges, HostListener, AfterViewChecked } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel, MetricMatchType, MetricAssetVersionConditionViewModel, MetricAssetDefinitionViewModel, MetricAssetDefinitionGovernanceViewModel, MetricAssetDefinitionGovernanceExternalViewModel, MetricUpdateFrequency, Condition } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from "../../../models/form.model";
import { FormHelpers } from '../../../static/form-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { OperatorModel, Operator } from '../../../models/operator.model';
import { FormGroup, FormBuilder, NgForm } from '@angular/forms';
import { CompanySettingsService } from '../../../services/settings.service';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { FieldTypeHelper } from '../../../models/fieldtype-api.model';
import { FieldTypeAPIModelFieldCondition, FieldCondition } from '../../../models/field-condition-grid.models';
import { FieldConditionGrid } from '../../shared/controls/field-condition-grid/field-condition-grid.component';


@Component({
    selector: 'd3s-admin-metric-editor',
    templateUrl: './admin-metric-editor.component.html',
    providers: [MetricsService, CompanySettingsService, FieldsObservableService],

})

export class AdminMetricEditorComponent extends BaseComponent implements OnInit, OnChanges, AfterViewChecked {
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


    verb = "Add";
    child = "";

    assetType: any = null;
    metricItem: any = null;
    conditionFormMode = FormMode.Default;
    FormMode = FormMode;
    private displayWeight: number = null;
    maxHeight: number = window.innerHeight - 160;
    maxScoreEffectiveDate: Date;
    currentEffectiveDate: Date;
    private date: Date;
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
    conditionForm: FormGroup = null;
    @ViewChild('conditionGrid', { static: false }) conditionGrid: FieldConditionGrid;

    private fields: any[] = [];

    constructor(private metricsService: MetricsService,
        protected messagesService: MessagesObservableService,
        private settingsService: CompanySettingsService,
        private fieldsService: FieldsObservableService,
        private fb: FormBuilder,
    ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['uid'] && (changes['uid'].currentValue != changes['uid'].previousValue)) {
            this.isLoading = true;
            this.load();
        }
        if (changes['parentUid'] && (changes['parentUid'].currentValue != changes['parentUid'].previousValue)) {
            this.isLoading = true;
            this.load();
        }
        if (changes['scoreData'] && (changes['scoreData'].currentValue != changes['scoreData'].previousValue)) {
            this.getMaxScoreDate();
        }
    }

    ngAfterViewChecked() {

        if (this.conditionGrid && !this.conditionForm) {
            this.conditionForm = this.fb.group(this.conditionGrid.condition_form);
        }
    }

    ngOnInit() {
        this.metricForm = this.fb.group({
            name: null,
            description: null,
            effectiveDate: null,
            weight: null,
            isGroup: null
        });

        this.load();
        this.loadFieldData();
    }

    loadFieldData() {
        this.settingsService.getOperators().subscribe(operators => {
            this.operators = operators;

            this.fieldsService.getFieldsV2(this.assetTypeUid, null, null).subscribe(res => {
                var tempFields = [];
                res.forEach(f => {
                    if (FieldTypeHelper.isFieldForOperator(f.Type)) {
                        tempFields.push(f as FieldTypeAPIModelFieldCondition);
                    }
                });

                tempFields.forEach(f => {
                    f.Operators = [];
                    this.operators.forEach(op => {
                        if (op.AllowedDataTypes.some(x => x.Name === FieldTypeHelper.getFieldType(f.Type))) {
                            f.Operators.push({ label: op.Name, value: op.ID });
                        }

                        if (FieldTypeHelper.getFieldType(f.Type) === 'Lookup') {
                            f.Values = [];
                            f.Values.push({ value: 'Value 1', label: 'Label 1' });
                            f.Values.push({ value: 'Value 2', label: 'Label 2' });
                            f.Values.push({ value: 'Value 3', label: 'Label 3' });
                            f.Values.push({ value: 'Value 4', label: 'Label 4' });
                            f.Values.push({ value: 'Value 5', label: 'Label 5' });
                            f.Values.push({ value: 'Value 6', label: 'Label 6' });
                        }

                        if (FieldTypeHelper.getFieldType(f.Type) === 'Boolean') {
                            f.Values = [];
                            f.Values.push({ value: 'true', label: 'True' });
                            f.Values.push({ value: 'false', label: 'False' });
                        }
                    });

                });

                this.fields = tempFields;
            });
        })
    }

    load() {
        if (!this.model)
            this.model = new MetricAssetViewModel();
        this.displayWeight = null;
        this.child = "";
        this.model.ParentUid = null;
        this.currentEffectiveDate = null;
        if (this.uid) {
            this.verb = "Edit"
            if (this.model.EffectiveDate !== null) {
                this.date = this.utcToLocal(new Date(this.model.EffectiveDate));
                this.currentEffectiveDate = this.model.EffectiveDate;
            }

            this.isLoading = false;
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
            this.isLoading = false;
        }
        if (this.model.Weight) {
            this.displayWeight = Math.round(this.model.Weight * 100);
        }

        if (!this.model.ConditionGroups || this.model.ConditionGroups.length === 0) {
            const dummyConditionGroup = new MetricAssetVersionConditionViewModel();
            dummyConditionGroup.Position = 1;
            dummyConditionGroup.MatchType = MetricMatchType.Any;
            this.model.ConditionGroups.push(dummyConditionGroup);
        }

        this.getMaxScoreDate();
        this.onResize(null);
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

    valid() {
        let valid = true;
        if (this.model === null) {
            valid = false;
        } else {
            if (this.model.Name === null || !this.model.Name) {
                valid = false;
            }
            else {
                if (this.model.Name.trim().length > 250 || this.model.Name.trim().length === 0) {
                    valid = false;
                }
            }

            if (this.date === null)
                valid = false;

            if (!this.isExternallyCalculated) {
                if (this.model.Weight === null || !this.model.Weight) {
                    valid = false;
                }
                else {
                    if (this.model.Weight === 0) {
                        valid = false;
                    }
                }
            }
        }

        return valid;
    }

    save() {
        this.isLoading = true;
        var prevDate: string | Date = null;
        var previousConditions = [...this.model.ConditionGroups];

        if (this.date !== null) {
            prevDate = this.date;
            let d = new Date(this.date);
            let condate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0);
            condate.setMinutes(condate.getMinutes() - condate.getTimezoneOffset());
            this.model.EffectiveDate = condate;
        }

        this.model.MatchConditionsOnly = true;

        this.model.Definition = new MetricAssetDefinitionViewModel();
        if (!this.isExternallyCalculated) {
            this.model.Definition.Governance = new MetricAssetDefinitionGovernanceViewModel();
            this.model.Definition.Governance.External = new MetricAssetDefinitionGovernanceExternalViewModel();
            this.model.Definition.Governance.External.UpdateFrequency = MetricUpdateFrequency.None;
        }

        this.model.ConditionGroups.forEach(g => {
            g.ConditionItems.forEach(c => {
                if (!c.Values) {
                    c.Values = [];
                }
                if (c.Values.length === 0) {
                    c.Values.push('');
                }
                switch (c.FieldType.Type) {
                    case 'Date':
                    case 'DateTime':
                        const d = new Date(c.SingleValue as string);
                        const condate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0);
                        condate.setMinutes(condate.getMinutes() - condate.getTimezoneOffset());
                        c.Values[0] = condate.toISOString();
                        break;
                    case 'Lookup':
                        c.Values[0] = c.SingleValue;
                        break;
                    default:
                        c.Values[0] = c.SingleValue;
                        break;
                }
            });
        });
        this.metricsService.saveMetric(this.model)
            .subscribe(r => {
                if (r) {
                    this.isLoading = false;
                    this.showMessageForResult(this.messagesService, r);
                    this.onSave.emit(this.model.Name);
                }
                else {
                    this.date = prevDate as Date;
                    this.model.ConditionGroups = [...previousConditions];
                    this.isLoading = false;
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
    }
};