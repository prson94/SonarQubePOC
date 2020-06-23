import { Input, Component, EventEmitter, Output, OnInit, ViewChild, ElementRef } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel, MetricMatchType, MetricAssetVersionConditionViewModel } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from "../../../models/form.model";
import { FormHelpers } from '../../../static/form-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service'; 


@Component({
    selector: 'd3s-admin-metric-editor',
    templateUrl: './admin-metric-editor.component.html',
    providers: [MetricsService]
})

export class AdminMetricEditorComponent extends BaseComponent implements OnInit {
    @Input() model: MetricAssetViewModel = null;
    @Input() allocationUid: string;
    @Input() uid: string;
    @Input() parentUid: string;
    @Input() isExternallyCalculated: boolean;

    @Input() metricEditorFieldTypes: MetricFieldTypeViewModel[] = [];

    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    @ViewChild('weight', { static: false }) weightInput: ElementRef;

    verb = "Add";
    child = "";

    assetType: any = null;
    metricItem: any = null;
    conditionFormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(private metricsService: MetricsService, protected messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        if (this.uid) {
            this.verb = "Edit"
            this.isLoading = false;

            if (this.model.EffectiveDate !== null) {
                this.model.EffectiveDate = new Date(this.model.EffectiveDate as string);
                this.model.EffectiveDate.setMinutes(this.model.EffectiveDate.getMinutes() + this.model.EffectiveDate.getTimezoneOffset());
            }
        } else {
            this.model = new MetricAssetViewModel();
            this.verb = "Add";
            this.isLoading = false;
            if (this.parentUid) {
                this.child = "Child";
                this.model.ParentUid = this.parentUid;
            }
            this.model.EffectiveDate = new Date();
            this.model.AllocationUid = this.allocationUid;
        }

        if (!this.model.ConditionGroups || this.model.ConditionGroups.length === 0) { 
            const dummyConditionGroup = new MetricAssetVersionConditionViewModel();
            dummyConditionGroup.Position = 1;
            dummyConditionGroup.MatchType = MetricMatchType.Any; 
            this.model.ConditionGroups.push(dummyConditionGroup);
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

            if (this.model.EffectiveDate === null)
                valid = false;

            if (!this.isExternallyCalculated) {
                if (this.model.Weight === null || !this.model.Weight) {
                    valid = false; 
                }
                else {
                    if (parseFloat(this.model.Weight.toFixed(2)) === 0) { 
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
        if (this.model.EffectiveDate !== null) {
            prevDate = this.model.EffectiveDate;
            let d = new Date(this.model.EffectiveDate as string);
            let condate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0);
            condate.setMinutes(condate.getMinutes() - condate.getTimezoneOffset());
            this.model.EffectiveDate = condate.toISOString();
        }

        this.model.ConditionGroups.forEach(g => {
            g.ConditionItems.forEach(c => {
                if (!c.Values) {
                    c.Values = [];
                }
                if (c.Values.length === 0) {
                    c.Values.push({ Value: '' });
                }
                switch (c.FieldType.Type) {
                    case 'Date':
                    case 'DateTime':
                        const d = new Date(c.SingleValue as string);
                        const condate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0);
                        condate.setMinutes(condate.getMinutes() - condate.getTimezoneOffset());
                        c.Values[0].Value = condate.toISOString();
                        break;
                    case 'Lookup':
                        c.Values[0].Value = c.SingleValue;
                        break;
                    default:
                        c.Values[0].Value = c.SingleValue;
                        break;
                }
            });
        });

        this.metricsService.saveMetric(this.model)
            .subscribe(r => {
                if (r) {
                    this.isLoading = false;
                    this.showMessageForResult(this.messagesService, r);
                    this.onSave.emit();
                }
                else {
                    this.model.EffectiveDate = prevDate;
                    this.isLoading = false;
                }
            });
    }

    cancel() {
        this.onCancel.emit();
    }

    getUTCDate(date: Date): Date {
        date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
        return date;
    }

    getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }

    private clamp(val: any, min: number, max: number, precision: number) {
        const newVal = FormHelpers.clamp(val, min, max, precision);

        if (this.weightInput !== null && this.weightInput.nativeElement !== null && this.weightInput.nativeElement !== undefined) 
            this.weightInput.nativeElement.value = newVal;

        this.model.Weight = newVal;
    }
};