import { Input, Component, EventEmitter, Output, OnInit, ViewChild, ElementRef, OnChanges, SimpleChanges, HostListener } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel, MetricMatchType, MetricAssetVersionConditionViewModel } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from "../../../models/form.model";
import { FormHelpers } from '../../../static/form-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service'; 


@Component({
    selector: 'd3s-admin-metric-editor',
    templateUrl: './admin-metric-editor.component.html',
    providers: [MetricsService],

})

export class AdminMetricEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() model: MetricAssetViewModel = null;
    @Input() allocationUid: string;
    @Input() uid: string;
    @Input() parentUid: string;
    @Input() isExternallyCalculated: boolean;
    @Input() scoreData: any;

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
    private displayWeight: number = null;
    maxHeight: number = window.innerHeight - 160;
    maxScoreEffectiveDate: Date;
    currentEffectiveDate: Date;
    private date: Date;
    measurestooltip: string = 'Asset conditions can be used to more specifically target assets of the chosen type to be scored by your measures. ' 
                                + 'Only those assets matching the conditions will be scored using these measures. '
                                + 'Where you use multiple conditions, you can specify whether an asset must match all or any of the conditions in order to be score by these measures';

    constructor(private metricsService: MetricsService, protected messagesService: MessagesObservableService) {
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

    ngOnInit() {
        this.load();
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
                    if (parseFloat(this.model.Weight.toFixed(2)) === 0) { 
                        valid = false;
                    }
                }

                if (this.model.ConditionGroups.length && !this.model.IsGroup) {
                    let conditions = this.model.ConditionGroups[0].ConditionItems;
                    if (conditions && conditions.length > 0) {
                        let fieldIds = conditions.map(x => { return x.ConditionFieldTypeID });
                        conditions.forEach(x => {
                            if (!x.ConditionFieldTypeID || !x.Operator || !x.SingleValue) {
                                valid = false;
                            }
                        });
                        if (fieldIds.some((item, inx) => { return fieldIds.indexOf(item) != inx })) {
                            valid = false;
                        }
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

    private clamp(val: any, min: number, max: number, precision: number) {
        if (!val) {
            this.model.Weight = null;
            this.valid();
            return;
        }
        val = val / 100;
        const newVal = FormHelpers.clamp(val, min, max, precision);

        if (this.weightInput !== null && this.weightInput.nativeElement !== null && this.weightInput.nativeElement !== undefined)
            this.weightInput.nativeElement.value = newVal;

        this.model.Weight = newVal;
    }
    doToggle(evt: MouseEvent, pc:any) {
        let htmlEl = evt.target as Element;
        if (htmlEl.classList.contains('ui-inputtext')) {
            evt.stopPropagation();
            return;
        }
        pc.toggle();
    }
    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.maxHeight = window.innerHeight - 240;
    }
};