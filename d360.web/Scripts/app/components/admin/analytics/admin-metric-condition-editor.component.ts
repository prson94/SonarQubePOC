import { Input, Component, EventEmitter, Output, OnInit, OnChanges } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import {  Condition, ConditionForm, MetricAssetVersionConditionViewModel, MetricFieldTypeViewModel } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-admin-metric-condition-editor',
    templateUrl: './admin-metric-condition-editor.component.html',
    providers: [MetricsService]
})

export class AdminMetricConditionEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() condition: MetricAssetVersionConditionViewModel = null;
    @Input() uid: string;
    @Input() metricConditionEditorFieldTypes: MetricFieldTypeViewModel[] = [];
    @Input() assetTypeUid: string;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    verb = "Add";

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {

    }

    getLookupValues(fieldTypeID: number) {
        return this.metricConditionEditorFieldTypes.find(i => i.ID == fieldTypeID).Values;
    }

    load() {
        if (this.condition) {
            if (this.condition.FieldTypeID) {
                this.selectFieldType();
            }
        }
        this.isLoading = false;
    }

    valid() {
        let valid = true;

        if (this.condition == null) {
            valid = false;
        } else {
        //    if (this.condition.MapID == null || this.condition.FieldTypeID == null || this.condition.FieldTypeID < 1) {
        //        valid = false;
        //    }
        //    if (this.condition.Value == null)
        //        valid = false;
        //    if (this.condition.Operator == null || this.condition.AndOr == null)
        //        valid = false;
        }

        return valid;
    }

    save() {

        if (this.condition.FieldType) {
            if (this.condition.FieldType.Type == "Lookup") {
                this.condition.ValuesText = this.condition.FieldType.Values.find(v => v.Value == +this.condition.Values).Text;
            }
            else {
                this.condition.ValuesText = this.condition.Values;
            }
        }
        this.onSave.emit(this.condition);
    }

    cancel() {
        this.onCancel.emit();
    }

    changeFieldType(e: any) {
        this.condition.FieldTypeID = +e;
        this.selectFieldType();
    }

    selectFieldType() {
        if (this.condition.FieldTypeID) {
            let field = this.metricConditionEditorFieldTypes.find(f => f.ID == this.condition.FieldTypeID);
            if (field != null) {
                this.condition.FieldTypeName = field.Name;
                this.condition.FieldType = field;
                if (!this.condition.Values) {
                    this.condition.Values = "";
                }
            }
        }
    }
};