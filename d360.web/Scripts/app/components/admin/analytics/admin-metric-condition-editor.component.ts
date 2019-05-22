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
    @Input() usedFieldTypes: number[] = [];
    @Input() assetTypeUid: string;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    verb = "Add";

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {

        //Set defaults;
        this.metricConditionEditorFieldTypes.forEach(ft => {
            ft.Disabled = false;
        });

        this.usedFieldTypes.forEach(i => {
            let ft = this.metricConditionEditorFieldTypes.find(ft => ft.ID == i);
            if (ft) {
                if (this.condition) {
                    if (this.condition.FieldTypeID != i) {
                        ft.Disabled = true;
                    } 
                }
                else {
                    ft.Disabled = true;
                }
            }
        });

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
        }

        return valid;
    }

    save() {

        if (this.condition.FieldType) {
            switch (this.condition.FieldType.Type) {
                case "Boolean":
                    this.condition.ValuesText = this.condition.Values.toString();
                    break;
                case "Lookup":
                    this.condition.ValuesText = this.condition.FieldType.Values.find(v => v.Value == +this.condition.Values).Text;
                    break;
                default:
                    this.condition.ValuesText = this.condition.Values;
                    break;
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
                switch (field.Type) {
                    case "Boolean":
                        this.condition.Values = (this.condition.Values == 'true') || (this.condition.Values == true);
                        break;
                    case "Date":
                    case "DateTime":
                        if (this.condition.Values) {
                            this.condition.Values = new Date(<string>this.condition.Values);
                            this.condition.Values.setMinutes(this.condition.Values.getMinutes() + this.condition.Values.getTimezoneOffset());
                        }
                        break;
                }
            }
        }
    }
};