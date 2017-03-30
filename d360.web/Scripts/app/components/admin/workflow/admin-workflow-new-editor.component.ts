import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import {
    WorkflowEventRegistration,
    WorkflowObjectType,
    WorkflowChangeType,
    ChangeTypeInfo,
    EventCondition,
    WorkflowListItem,
    WorkflowDiagramModel,
} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';

@Component({
    selector: 'd3s-admin-workflow-new-editor',
    providers: [WorkflowService],
    templateUrl: './admin-workflow-new-editor.component.html'
})

export class AdminWorkflowNewEditorComponent extends BaseComponent implements OnInit {
    @Input() id: number = 0;
    @Output() onClose = new EventEmitter();
    @Output() onSave = new EventEmitter();

    private model: WorkflowDiagramModel;
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
            this.model = new WorkflowDiagramModel();

    }

    load() {
        this.isLoading = true;

        this.workflowService.getWorkflowObjectTypes()
            .then(r => { this.workflowObjectTypes = r; })
            .then(() => this.workflowService.getChangeTypes())
            .then(r => { this.changesTypes = r; })
            .then(() => {
                if (this.id < 1) {
                    this.saveButtonText = 'Next';
                    return;
                } else {
                    this.saveButtonText = 'Save';
                    return this.workflowService.getWorkflowTypeModel(this.id)
                        .then(r => {
                            this.model = r

                            this.selectedObjectType = this.model.Event.Object + '|' + this.model.Event.ObjectID.toString();
                            this.objectId = this.model.Event.ObjectID;
                            this.objectType = this.model.Event.Object;

                            console.log(r);

                            if (this.model.Event.ConditionObject != null && this.model.Event.ConditionObject.Conditions != null) {
                                let cond = [];
                                if (this.model.Event.ConditionObject.Conditions.length == null)
                                    cond.push(this.model.Event.ConditionObject.Conditions.Condition);
                                else
                                    cond = this.model.Event.ConditionObject.Conditions.Condition;

                                cond.forEach(c => {
                                    this.conditions.push({
                                        fieldName: '',
                                        FieldTypeID: c['@FieldTypeID'],
                                        Operator: c['@Operator'],
                                        Value: c['@Value'],
                                        ValueType: c['@ValueType']
                                    });
                                });
                            }
                        })
                        .then(() => this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType))
                        .then(r => {
                            //need to apply names to loaded conditions
                            r.forEach(t => {
                                let c = this.conditions.find(c => c.FieldTypeID == t.ID);
                                if (c != null)
                                    c.fieldName = t.FriendlyName;
                            })
                        });
                }
            })
            .then(() => this.isLoading = false);

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
        this.model.Event.Object = this.objectType;
        this.model.Event.ObjectID = this.objectId;
        this.model.Event.Condition = JSON.stringify(this.conditions);

        this.isLoading = true;
        this.workflowService.saveWorkflowDiagramModel(this.model)
            .then(r => {
                this.isLoading = false;
                this.model.Type.ID = r;
                this.onSave.emit(this.model);

            });
    }
}