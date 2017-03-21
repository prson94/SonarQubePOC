import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import {
    WorkflowEventRegistration,
    EventCondition,
} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-workflow-new-condition-editor',
    providers: [WorkflowService],
    template: `
                        <div class="row">
                            <div class="col s12">
                                <div class="FieldName">
                                    Field
                                </div>
                                <div>
                                    <select style="width:95%;" placeholder="Choose a value" [ngModel]="condition.FieldTypeID" (ngModelChange)="selectFieldType($event);">
                                        <option></option>
                                        <option *ngFor="let i of fields" [value]="i.ID">{{i.FriendlyName}}</option>
                                    </select>
                                </div>
                                <div class="FieldName">
                                    Operator
                                </div>
                                <div>
                                    <select style="width:95%;" placeholder="Choose a value" [(ngModel)]="condition.Operator" [disabled]="!(condition.FieldTypeID > 0)">
                                        <option></option>
                                        <option *ngFor="let i of operators" [value]="i.value">{{i.label}}</option>
                                    </select>
                                </div>
                                <div *ngIf="condition.FieldTypeID > 0" [ngSwitch]="selectedField.Type">
                                    <div *ngSwitchCase="'Boolean'">
                                        <div class="FieldName">
                                            Value
                                        </div>
                                        <div>
                                            <select style="width:95%;" placeholder="Choose a value" [(ngModel)]="condition.Value">
                                                <option></option>
                                                <option *ngFor="let b of bool" [value]="b.value">{{b.label}}</option>
                                            </select>
                                        </div>
                                    </div>
                                    <div *ngSwitchCase="'Date'">
                                        <div class="FieldName">
                                            Days Since
                                        </div>
                                        <div>
                                            <input type="number" [(ngModel)]="condition.Value" style="width: 95%" />
                                        </div>
                                    </div>
                                    <div *ngSwitchCase="'DateTime'">
                                        <div class="FieldName">
                                            Days Since
                                        </div>
                                        <div>
                                            <input type="number" [(ngModel)]="condition.Value" style="width: 95%" />
                                        </div>
                                    </div>
                                    <div *ngSwitchCase="'Lookup'">
                                        <div class="FieldName">
                                            Value
                                        </div>
                                        <div>
                                            lookup
                                        </div>
                                    </div>
                                    <div *ngSwitchCase="'FusionLookup'">
                                        <div class="FieldName">
                                            Value
                                        </div>
                                        <div>
                                            fusion lookup
                                        </div>
                                    </div>
                                    <div *ngSwitchCase="'Decimal'">
                                        <div class="FieldName">
                                            Value
                                        </div>
                                        <div>
                                            <input type="number" [(ngModel)]="condition.Value" style="width: 95%" />
                                        </div>
                                    </div>
                                    <div *ngSwitchCase="'Number'">
                                        <div class="FieldName">
                                            Value
                                        </div>
                                        <div>
                                            <input type="number" [(ngModel)]="condition.Value" style="width: 95%" />
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                &nbsp;
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12" style="float: right">
                                <button type="button" pButton label="Cancel" (click)="close()"></button>
                                <button type="button" pButton label="Add" (click)="save()" [disabled]="condition.Value == null || condition.Operator == null || condition.FieldTypeID == 0"></button>
                            </div>
                        </div>
`
})

export class AdminWorkflowNewConditionEditorComponent extends BaseComponent implements OnInit {
    @Input() objectType: string;
    @Input() objectId: number;
    @Output() onSave = new EventEmitter();
    @Output() onClose = new EventEmitter();


    private condition = new EventCondition();
    private fields: FieldType[] = [];
    private selectedField: FieldType = new FieldType();

    private operators = [
        { value: '=', label: 'equal to' },
        { value: '!=', label: 'not equal to' },
        { value: '>', label: 'greater than' },
        { value: '<', label: 'less than' },
        { value: '>=', label: 'greater than or equal to' },
        { value: '<=', label: 'less than or equal to' },
    ];

    private bool = [
        { value: 'true', label: 'True' },
        { value: 'false', label: 'False' }
    ];

    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.setOperators();
        this.load();
    }

    load() {
        this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType)
            .then(r => {
                this.fields = r;
            });
    }

    save() {
        this.onSave.emit(this.condition);
    }

    close() {
        this.onClose.emit();
    }


    selectFieldType(e: any) {
        this.condition.FieldTypeID = e;
        this.selectedField = this.fields.find(f => f.ID == e);
        this.condition.fieldName = this.selectedField.FriendlyName;
        this.setOperators(this.selectedField.Type);
    }

    setOperators(type: string = '') {
        switch (type) {
            case 'Boolean':
            case 'Lookup':
            case 'FusionLookup':
                this.operators = [
                    { value: '=', label: 'equal to' },
                    { value: '!=', label: 'not equal to' },
                ];
                break;
            case 'Decimal':
            case 'Number':
            case 'Date':
            case 'DateTime':
            default:
                this.operators = [
                    { value: '=', label: 'equal to' },
                    { value: '!=', label: 'not equal to' },
                    { value: '>', label: 'greater than' },
                    { value: '<', label: 'less than' },
                    { value: '>=', label: 'greater than or equal to' },
                    { value: '<=', label: 'less than or equal to' },
                ];
                break;
        }
    }

}