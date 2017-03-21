import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import {
    WorkflowEventRegistration,
    WorkflowObjectType,
    WorkflowChangeType,
    ChangeTypeInfo,
    EventCondition,
    WorkflowTypeItem,
    WorkflowTypeModel,
} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-workflow-new-editor',
    providers: [WorkflowService],
    templateUrl: './admin-workflow-new-editor.component.html'
})

export class AdminWorkflowNewEditorComponent extends BaseComponent implements OnInit {
    @Input() model: WorkflowTypeModel;
    @Output() modelChange = new EventEmitter();
    @Output() onClose = new EventEmitter();
    @Output() onSave = new EventEmitter();

    private workflowObjectTypes: WorkflowObjectType[] = [];
    private changesTypes: ChangeTypeInfo[] = [];
    private selectedObjectType: any = null;
    private conditions: EventCondition[] = [];

    private showAddCondition: boolean = false;
    private objectType: string;
    private objectId: number;
    private saveButtonText: string = 'Next';

    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.load();
        if (this.model == null)
            this.model = new WorkflowTypeModel();

    }

    load() {
        this.isLoading = true;

        this.workflowService.getWorkflowObjectTypes()
            .then(r => { this.workflowObjectTypes = r; })
            .then(() => this.workflowService.getChangeTypes())
            .then(r => { this.changesTypes = r; })
            .then(() => {
                if (this.model.Type.ID < 1) {
                    this.saveButtonText = 'Next';
                } else {
                    this.saveButtonText = 'Save';
                    //edit
                }
            })

    }

    selectObjectType(e: any) {
        this.selectedObjectType = e;
        this.showAddCondition = false;
        this.conditions = [];

        if (e.indexOf('|') < 0)
            return;

        this.objectType = e.split('|')[0];
        this.objectId = +e.split('|')[1];

    }

    showCondition() {
        if (this.showAddCondition)
            return;
        this.showAddCondition = true;
    }

    addCondition(e: EventCondition) {
        this.conditions.push(e);
        this.showAddCondition = false;
        console.log(this.conditions);
    }

    remove(item: EventCondition) {
        let i = this.conditions.findIndex(c => c == item);
        this.conditions.splice(i, 1);
    }

    save() {
        this.model.Event.conditions = this.conditions;
        this.onSave.emit(this.model);
    }
}