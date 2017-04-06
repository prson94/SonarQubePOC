import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import {
    NodeModel,
    WorkflowForm,
    WorkflowFormField,
    WorkflowFormFieldType,
    FormResponseType,
} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header, MenuItem } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-step-form-editor',
    providers: [WorkflowService],
    template: `
<div class="row">
    <div class="col s12">
        <div class="FieldName">
            Title
        </div>
        <div>
            <input type="text" [ngModel]="step.fields.form['@title']" (ngModelChange)="step.fields.form['@title']=$event; stepChange.emit(step);" style="width: 95%" />
        </div>
    </div>
</div>
<div class="row">
    <div class="col s12">
        <div class="FieldName">
            Description
        </div>
        <div>
            <input type="text" [ngModel]="step.fields.form['@description']" (ngModelChange)="step.fields.form['@description']=$event; stepChange.emit(step);" style="width: 95%" />
        </div>
    </div>
</div>
<div class="row">
    <div class="col s12">
        <div class="FieldName">
            Response Type
        </div>
        <div>
            <select [ngModel]="step.settings.FormResponseType" (ngModelChange)="step.settings.FormResponseType = $event; stepChange.emit(step);" style="width: 95%">
                <option *ngFor="let r of responseTypes" [value]="r.value">{{r.label}}</option>
            </select>
        </div>
    </div>
</div>
<div class="row">
    <div class="col s12">   
        <header>
            &nbsp;
            <d3s-tile-actions hasAdd="true" (addClick)="add()"></d3s-tile-actions>
        </header>
        <p-dataTable [value]="step.fields.form.field" selectionMode="single">
            <p-column field="@id" header="Name"></p-column>
            <p-column field="@label" header="Label"></p-column>
            <p-column field="@type" header="Type"></p-column>
            <p-column>
                <template let-item="rowData" pTemplate type="body">
                    <div class="RowTools">
                        <a style="cursor:pointer;" (click)="remove(item)"><i class="fa fa-trash"></i></a>
                    </div>
                </template>
            </p-column>
        </p-dataTable>
    </div>
</div>
<div *ngIf="isAdding">
    <div class="row">
        <div class="col s12">
            <div class="FieldName">
                Field Type
            </div>
            <div>
                <select [(ngModel)]="newField['@type']" style="width:95%">
                    <option *ngFor="let t of types" [value]="t.label">{{t.label}}</option>
                </select>
            </div>
        </div>
    </div>
    <div class="row">
        <div class="col s12">
            <div class="FieldName">
                Label
            </div>
            <div>
                <input type="text" [(ngModel)]="newField['@label']" style="width: 95%" />
            </div>
        </div>
    </div>
    <div class="row" style="padding-top: 8px;">
        <div class="col s12">
            <button pButton type="button" label="Add" (click)="save()" [disabled]="newField['@label'] == null || newField['@label'].length < 1 || newField['@type'] == null"></button>
            <button pButton type="button" label="Cancel" (click)="cancel()"></button>
        </div>
    </div>
</div>
`
})

export class WorkflowStepFormEditorComponent extends BaseComponent implements OnInit {
    @Input() step: NodeModel;
    @Output() stepChange = new EventEmitter();

    private model: WorkflowForm = new WorkflowForm();

    private originalStep: NodeModel;
    WorkflowFormFieldType = WorkflowFormFieldType;
    private newField: any = {};
    private isAdding = false;

    private types = [
        { value: WorkflowFormFieldType.Boolean, label: 'boolean' },
        { value: WorkflowFormFieldType.Integer, label: 'integer' },
        { value: WorkflowFormFieldType.Text, label: 'text' },
        { value: WorkflowFormFieldType.Date, label: 'date' },

    ];

    FormResponseType = FormResponseType;

    private responseTypes = [
        { value: FormResponseType[FormResponseType.FirstResponse], label: 'First Response' },
        { value: FormResponseType[FormResponseType.Majority], label: 'Majority' },
        { value: FormResponseType[FormResponseType.All], label: 'All' },
    ];

    constructor() {
        super();
    }

    ngOnInit() {
        this.originalStep = _.cloneDeep(this.step);


        //deal with xml-json nonsense
        if (this.step.fields == null || this.step.fields.form == null) {
            this.step.fields = {};
            this.step.fields.form = {};
            this.step.fields.form.field = [];
        }
        if (this.step.fields.form.field == null) {
            this.step.fields.form.field = [];
        }

        if (this.step.fields.form.field.length == null) {
            let f = _.cloneDeep(this.step.fields.form.field);
            this.step.fields.form.field = [];
            this.step.fields.form.field.push(f);
        }

        //console.log('step: ', this.step);
    }

    add() {
       // console.log(this.step);
        this.isAdding = true;
    }

    remove(item: any) {
        let i = this.step.fields.form.field.findIndex(f => f['@id'] == item['@id']);
        if (i >= 0) {
            this.step.fields.form.field.splice(i, 1);
            this.stepChange.emit(this.step);
        }
    }

    cancel() {
        this.isAdding = false;
        this.newField = {};
    }

    save() {

        //let count = this.model.Fields.filter(f => f.FieldType == this.newField.type).length + 1;

        let count = this.step.fields.form.field.filter(f => f['@type'] == this.newField['@type']).length + 1;

        this.newField['@id'] = this.newField['@type'].toString().toLowerCase() + count.toString();

        this.step.fields.form.field.push(_.cloneDeep(this.newField));
        //this.model.Fields.push(_.cloneDeep(this.newField));
        this.newField = {};
        this.isAdding = false;
        this.stepChange.emit(this.step);
    }

}