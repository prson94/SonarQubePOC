import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import {
    WorkflowEventRegistration,
    WorkflowObjectType,
    WorkflowChangeType,
    ChangeTypeInfo,
    EventCondition,
    WorkflowListItem,
    WorkflowDiagramModel,
    WorkflowDiagramNode,
    NodeModel,
    WorkflowActivityType,
    WorkflowTaskProcedure,

} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-step-editor',
    providers: [WorkflowService, ResponsibilityTypeService],
    template: `
<div class="row">
    <div class="col s12">
        <div class="FieldName">Name</div>
        <div>
            <input type="text" style="width: 95%" [ngModel]="step.name" (ngModelChange)="step.name = $event; stepChange.emit(step)" />
        </div>
    </div>
    <div [ngSwitch]="step.activityType">
        <div class="row" *ngSwitchCase="WorkflowActivityType.EmailNotification">
            <div class="col s12">
                <div class="FieldName">
                    Recipient Type
                </div>
                <div>
                    <select [ngModel]="step.settings.MessageRecipientType" (ngModelChange)="step.settings.MessageRecipientType = $event; stepChange.emit(step)" style="width: 95%">
                        <option *ngFor="let d of destination" [value]="d.value">{{d.label}}</option>
                    </select>
                </div>
            </div>
            <div *ngIf="step.settings.MessageRecipientType == 'SpecificUser'" class="col s12">
                <div class="FieldName">
                    Recipient
                </div>
                <div>
                    <input type="text" [ngModel]="step.settings.MessageToUser" (ngModelChange)="step.settings.MessageToUser = $event; stepChange.emit(step)" style="width: 95%" />
                </div>
            </div>
            <div *ngIf="step.settings.MessageRecipientType == 'Owner'" class="col s12">
                <div class="FieldName">
                    Owner
                </div>
                <div>
                    <select [ngModel]="step.settings.ResponsibilityTypeID" (ngModelChange)="step.settings.ResponsibilityTypeID = $event; stepChange.emit(step)" style="width: 95%">
                        <option></option>
                        <option *ngFor="let r of responsibilities" [value]="r.ID">{{r.Name}}</option>
                    </select>
                </div>
            </div>
            <div class="col s12">
                <div class="FieldName">
                    Subject
                </div>
                <div>
                    <input type="text" style="width:95%" [ngModel]="step.settings.MessageSubjectTemplate" (ngModelChange)="step.settings.MessageSubjectTemplate = $event; stepChange.emit(step)" />
                </div>
            </div>
            <div class="col s12">
                <div class="FieldName">
                    Body
                </div>
                <div style="width: 95%">
                    <p-editor [ngModel]="step.settings.MessageBodyTemplate" (ngModelChange)="step.settings.MessageBodyTemplate = $event; stepChange.emit(step)"></p-editor>
                </div>
            </div>
            <div class="col s12">
                <div>
                    <input type="checkbox" [ngModel]="step.settings.IncludePreviousFormResponses" (ngModelChange)="step.settings.IncludePreviousFormResponses = $event; stepChange.emit(step)" /> Include previous form responses
                </div>
            </div>
        </div>
        <div class="row" *ngSwitchCase="WorkflowActivityType.StatusChange">
            <div class="col s12">
                <div class="FieldName">Status</div>
                <div>
                    <select [ngModel]="step.settings.Status" (ngModelChange)="step.settings.Status = $event; stepChange.emit(step)" style="width: 95%">
                        <option value=""></option>
                        <option *ngFor="let s of status" [value]="s">{{s}}</option>
                    </select>
                </div>
            </div>
        </div>
        <div class="row" *ngSwitchCase="WorkflowActivityType.Form">
            <d3s-workflow-step-form-editor [step]="step" (stepChange)="step = $event; stepChange.emit(step)"></d3s-workflow-step-form-editor>
        </div>
        <div class="row" *ngSwitchCase="WorkflowActivityType.Procedure">
            <div class="col s12">
                <div class="FieldName">Procedure</div>
                <div>
                    <select style="width: 95%" [ngModel]="step.settings.ProcedureID" (ngModelChange)="step.settings.ProcedureID = $event; stepChange.emit(step);">
                        <option *ngFor="let p of procedures" [value]="p.ID">{{p.Name}} ({{p.Procedure}})</option>
                    </select>
                </div>
            </div>
        </div>
    </div>
    <div *ngIf="step.hasMultipleInputs" class="col s12" style="padding-top: 8px">
        <input type="checkbox" [ngModel]="step.settings.WaitForAllTransitions" (ngModelChange)="step.settings.WaitForAllTransitions = $event; stepChange.emit(step)" /> Wait for all transitions to complete?
    </div>
</div>
`
})

export class WorkflowStepEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() step: NodeModel;
    @Output() stepChange = new EventEmitter();

    WorkflowActivityType = WorkflowActivityType;

    private originalStep: NodeModel;
    private status = [
        'Draft',
        'Under Review',
        'Certified'
    ];

    private destination = [
        { value: 'Initiator', label: 'Initiator' },
        { value: 'Owner', label: 'Owner' },
        { value: 'SpecificUser', label: 'Specific User' },
    ];

    private responsibilities = [];
    private procedures: WorkflowTaskProcedure[] = [];

    constructor(private responsibilityService: ResponsibilityTypeService, private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
    }

    ngOnChanges() {
        if (this.step.settings == null)
            this.step.settings = {};
        this.originalStep = _.cloneDeep(this.step);

        if (this.step.activityType == WorkflowActivityType.EmailNotification) {
            this.responsibilityService.getResponsibilityTypes()
                .then(r => {
                    this.responsibilities = r;
                    console.log(r);
                });
        } else if (this.step.activityType == WorkflowActivityType.Procedure) {
            this.workflowService.getWorkflowProcedures()
                .then(r => {
                    this.procedures = r;
                });
        }
    }

}