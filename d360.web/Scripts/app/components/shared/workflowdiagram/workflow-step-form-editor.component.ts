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
import { WorkflowFieldsService } from '../../../services/workflow-fields.service';
import { FormMode } from '../../../models/form.model';

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
<div *ngIf="formMode == FormMode.Adding">
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
<div *ngIf="formMode == FormMode.Deleting">
    <div class="row" *ngIf="usedIn.length < 1">
        <div class="col s12">
            <div>
                Are you sure you want to delete the {{deletingField['@id']}} field?
            </div>
            <div>
                <button pButton type="button" label="Delete" (click)="confirmDelete()"></button>
                <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
            </div>
        </div>
    </div>
    <div class="row" *ngIf="usedIn.length > 0">
        <div class="col s12">
            <div>
                The field {{deletingField['@id']}} cannot be deleted because it is used in the following transition conditions:
            </div>
            <div *ngFor="let u of usedIn" style="margin-left: 8px;">
               &bull; {{(u.transitionName == '') ? '[No name]' : u.transitionName }}
            </div>
            <div>
                <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
            </div>
        </div>
    </div>
</div>
`
})

export class WorkflowStepFormEditorComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() step: NodeModel;
    @Output() stepChange = new EventEmitter();

    private model: WorkflowForm = new WorkflowForm();

    private originalStep: NodeModel;
    WorkflowFormFieldType = WorkflowFormFieldType;
    FormMode = FormMode;
    private newField: any = {};
    private formMode = FormMode.Default;
    private usedIn: any[] = [];
    private deletingField;

    private usedFields: any[] = [];

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

    private fieldsSub: any;

    constructor(private workflowFieldsService: WorkflowFieldsService) {
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

        this.usedFields = this.workflowFieldsService.getUsedFields();

    }

    ngOnDestroy() {
        //this.fieldsSub.unsubscribe();
    }

    add() {
        this.formMode = FormMode.Adding;
    }

    remove(item: any) {
        this.deletingField = item;

        this.usedIn = [];
        this.usedIn = this.usedFields.filter(u => u.stepId == this.step.key && u.fieldId == item['@id']);

        this.formMode = FormMode.Deleting;
    }

    confirmDelete() {
        let i = this.step.fields.form.field.findIndex(f => f['@id'] == this.deletingField['@id']);

        if (i >= 0) {
            this.step.fields.form.field.splice(i, 1);
            this.stepChange.emit(this.step);
            this.workflowFieldsService.deleteFormField(this.deletingField);
        }

        this.formMode = FormMode.Default;
    }

    cancel() {
        this.formMode = FormMode.Default;
        this.newField = {};
    }

    save() {
        let count = this.step.fields.form.field.filter(f => f['@type'] == this.newField['@type']).length + 1;

        this.newField['@id'] = this.newField['@type'].toString().toLowerCase() + count.toString();

        let f = {};
        f['@id'] = this.newField['@id'];
        f['@label'] = this.newField['@label'];
        f['@type'] = this.newField['@type'];
        f['@stepId'] = this.step.key;

        this.step.fields.form.field.push(_.cloneDeep(this.newField));
        this.newField = {};
        this.formMode = FormMode.Default;
        this.stepChange.emit(this.step);


        this.workflowFieldsService.pushFormField(f);
    }

}