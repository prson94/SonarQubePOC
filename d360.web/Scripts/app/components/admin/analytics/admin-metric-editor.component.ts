import { Input, Component, EventEmitter, Output, OnInit, ViewChild, ElementRef } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, MetricFieldTypeViewModel } from '../../../models/metrics.model';
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
    @Input() assetTypeUid: string;
    @Input() uid: string;
    @Input() parentUid: string;

    @Input() metricEditorFieldTypes: MetricFieldTypeViewModel[] = [];

    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    @ViewChild('weight') weightInput: ElementRef;

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

            if (this.model.EffectiveDate != null) {
                this.model.EffectiveDate = new Date(<string>this.model.EffectiveDate);
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
            this.model.AssetTypeUid = this.assetTypeUid;
        }
    }

    valid() {
        let valid = true;

        //if (this.model == null || this.model.Map == null) {
        //    valid = false;
        //} else {
        //    if (this.model.Map.AssetTypeID == null)
        //        valid = false;
        //    if (this.model.Map.ItemID == null || this.model.Map.ItemID < 1)
        //        valid = false;
        //    if ((<any>this.model.Map).Weight == "" || this.model.Map.Weight == null || this.model.Map.Weight < 0 || this.model.Map.Weight > 1)
        //        valid = false;
        //    if (this.model.Map.EffectiveDate == null)
        //        valid = false;
        //}

        return valid;
    }

    save() {
        this.isLoading = true;
        var prevDate: string | Date = null;
        if (this.model.EffectiveDate != null) {
            prevDate = this.model.EffectiveDate;
            this.model.EffectiveDate = new Date(<string>this.model.EffectiveDate).toISOString();
        }
            

        this.metricsService.saveMetric(this.model)
            .subscribe(r => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, r);
                this.onSave.emit();
            },
            e => {
                this.model.EffectiveDate = prevDate;
                this.isLoading = false;
            },
            () => {
                console.log('complete');
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

    private clamp(val: any, min: number, max: number, precision: number): any {
        let newVal = FormHelpers.clamp(val, min, max, precision);

        if (this.weightInput != null && this.weightInput.nativeElement != null)
            this.weightInput.nativeElement.value = newVal;

        return newVal;
    }
};