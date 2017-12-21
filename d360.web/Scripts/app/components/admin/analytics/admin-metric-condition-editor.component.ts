import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import {  Condition, ConditionForm } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-admin-metric-condition-editor',
    template: ` 
                <header>{{verb}} Mapping</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading">
                    <div class="row">
                        <div class="col s3">
                            <div class="FieldName">
                                Field
                            </div>
                            <div>
                                <select [(ngModel)]="model.Condition.FieldTypeID" style="width: 95%">
                                    <option></option>
                                    <option *ngFor="let i of model.Fields" [value]="i.ID">{{i.FriendlyName}}</option>
                                </select>
                            </div>
                        </div>
                        <div class="col s2">
                            <div class="FieldName">
                                Operator
                            </div>
                            <div>
                                <select [(ngModel)]="model.Condition.Operator" style="width: 95%">
                                    <option></option>
                                    <option *ngFor="let i of operators" [value]="i.value">{{i.label}}</option>
                                </select>
                            </div>
                        </div>
                        <div class="col s4">
                            <div class="FieldName">
                                Value
                            </div>
                            <div>
                                <input type="text" style="width: 95%" [(ngModel)]="model.Condition.Value" />
                            </div>
                        </div>   
                        <div class="col s3">
                            <div class="FieldName">
                                And/Or
                            </div>
                            <div>
                                <select [(ngModel)]="model.Condition.AndOr" style="width: 95%">
                                    <option></option>
                                    <option *ngFor="let i of andOr" [value]="i.value">{{i.label}}</option>
                                </select>
                            </div>
                        </div>  
                        <div class="col s12" style="padding-top: 15px">
                            <button pButton type="button" label="Add" [disabled]="!valid()" (click)="save()"></button>
                            <button pButton type="button" label="Cancel" (click)="cancel()"></button>
                        </div>
                    </div>
                </div>
                `,
    providers: [MetricsService, MessagesService]
})

export class AdminMetricConditionEditorComponent extends BaseComponent implements OnInit {
    @Input() mapId: number = -1;
    @Input() fieldId: number = -1;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    verb = "Add";

    model: ConditionForm;

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

    load() {
        if (this.fieldId > 0) {
            this.verb = "Edit"
            this.isLoading = true;
            this.metricsService.getCondition(this.mapId, this.fieldId)
                .then(r => {
                    this.model = r;
                    this.isLoading = false;
                });
        } else {
            this.verb = "Add";
            this.isLoading = true;
            this.metricsService.getCondition(this.mapId, this.fieldId)
                .then(r => {
                    this.model = r;
                    this.model.Condition = new Condition();
                    this.model.Condition.MapID = this.mapId;
                    this.isLoading = false;
                });

        }
    }

    valid() {
        let valid = true;

        if (this.model == null || this.model.Condition == null) {
            valid = false;
        } else {
            if (this.model.Condition.MapID == null || this.model.Condition.FieldTypeID == null || this.model.Condition.FieldTypeID < 1 || this.model.Condition.MapID < 1) {
                valid = false;
            }
            if (this.model.Condition.Value == null)
                valid = false;
            if (this.model.Condition.Operator == null || this.model.Condition.AndOr == null)
                valid = false;
        }

        return valid;
    }

    save() {
        this.isLoading = true;
        this.metricsService.saveCondition(this.model.Condition)
            .then(r => {
                this.showMessageForResult(this.messagesService, r);
                this.isLoading = false;
                this.onSave.emit();
            });
    }

    cancel() {
        this.onCancel.emit();
    }


};