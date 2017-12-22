import { Input, Component, EventEmitter, Output, OnInit, OnChanges } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import {  Condition, ConditionForm } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-admin-metric-condition-editor',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading">
                    <div class="row">
                        <div class="col s3">
                            <div class="FieldName">
                                Field
                            </div>
                            <div>
                                <select [ngModel]="condition.FieldTypeID" (ngModelChange)="changeFieldType($event)" style="width: 95%">
                                    <option></option>
                                    <option *ngFor="let i of fields" [value]="i.ID">{{i.FriendlyName}}</option>
                                </select>
                            </div>
                        </div>
                        <div class="col s2">
                            <div class="FieldName">
                                Operator
                            </div>
                            <div>
                                <select [(ngModel)]="condition.Operator" style="width: 95%">
                                    <option></option>
                                    <option *ngFor="let i of operators" [value]="i.value">{{i.label}}</option>
                                </select>
                            </div>
                        </div>
                        <div class="col s4">
                            <div class="FieldName">
                                Value
                            </div>
                            <div *ngIf="fieldType != 'Lookup' && fieldType != 'Number' && fieldType != 'Decimal'">
                                <input type="text" style="width: 95%" [(ngModel)]="condition.Value" />
                            </div>
                            <div *ngIf="fieldType == 'Lookup'">
                                <select style="width:95%;" placeholder="Choose a value" [(ngModel)]="condition.Value">
                                    <option></option>
                                    <option *ngFor="let l of lookups" [value]="l.value">{{l.label}}</option>
                                </select>
                            </div>
                            <div *ngIf="fieldType == 'Number' || fieldType == 'Decimal'">
                                <input type="number" [(ngModel)]="condition.Value" style="width: 95%" />
                            </div>
                        </div>   
                        <div class="col s3">
                            <div class="FieldName">
                                And/Or
                            </div>
                            <div>
                                <select [(ngModel)]="condition.AndOr" style="width: 95%">
                                    <option></option>
                                    <option *ngFor="let i of andOr" [value]="i.value">{{i.label}}</option>
                                </select>
                            </div>
                        </div>  
                        <div class="col s12" style="padding-top: 15px;">
                            <button pButton type="button" label="Cancel" (click)="cancel()" style="float: right"></button>
                            <button pButton type="button" label="Add" [disabled]="!valid()" (click)="save()" style="float: right"></button>
                        </div>
                    </div>
                </div>
                `,
    providers: [MetricsService]
})

export class AdminMetricConditionEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() condition = null;
    @Input() mapId: number = -1;
    @Input() fieldId: number = -1;
    @Input() objectType: string = "";
    @Input() objectId: number = -1;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    verb = "Add";
    model: ConditionForm;
    fields = [];
    lookups = [];
    fieldType = "";

    private operators = [
        { value: 'eq', label: '=' },
        { value: 'neq', label: '!=' },
        { value: 'lt', label: '<' },
        { value: 'lte', label: '<=' },
        { value: 'gt', label: '>' },
        { value: 'gte', label: '>=' },
    ];

    private andOr = [
        { value: 'a', label: 'And' },
        { value: 'o', label: 'Or' },
    ];

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {
        if (this.fieldId < 1 && this.objectId > 0 && this.model != null) {
            this.metricsService.getConditionFields(this.objectType, this.objectId)
                .then(r => {
                    this.fields = r;
                });
        }
    }

    load() {
        this.fields = [];
        this.metricsService.getConditionFields(this.objectType, this.objectId)
            .then(r => {
                this.fields = r;
            });
        if (this.condition != null)
            this.changeFieldType(this.condition.FieldTypeID);

    }

    valid() {
        let valid = true;

        if (this.condition == null) {
            valid = false;
        } else {
            if (this.condition.MapID == null || this.condition.FieldTypeID == null || this.condition.FieldTypeID < 1) {
                valid = false;
            }
            if (this.condition.Value == null)
                valid = false;
            if (this.condition.Operator == null || this.condition.AndOr == null)
                valid = false;
        }

        return valid;
    }

    save() {
        this.onSave.emit(this.condition);
    }

    cancel() {
        this.onCancel.emit();
    }

    changeFieldType(e: any) {
        this.condition.FieldTypeID = +e;
        
        let field = this.fields.find(f => f.ID == +e);
        if (field != null) {
            this.fieldType = field.Type;
            this.condition.fieldName = field.FriendlyName;
            //console.log('changeFieldType', this.condition, field);

            if (field.Type == 'Lookup') {
                this.lookups = [];
                this.metricsService.getLookupList(this.condition.FieldTypeID)
                    .then(r => {
                        this.lookups = r;
                    })

            }
        }
    }


};