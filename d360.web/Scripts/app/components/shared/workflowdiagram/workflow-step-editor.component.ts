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
    WorkflowDiagramNode,
    NodeModel,
    
} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-step-editor',
    providers: [WorkflowService],
    template: `
<div class="row">
    <div class="col s12">
        <div class="FieldName">Name</div>
        <div>
            <input type="text" style="width: 95%" [ngModel]="step.name" (ngModelChange)="step.name = $event; stepChange.emit(step)" />
        </div>
    </div>
    <div [ngSwitch]="step.activityType">
        <div class="row" *ngSwitchCase="1">
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
                    <select>
                        <option></option>
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
                <div>
                    <input type="text" style="width:95%" [ngModel]="step.settings.MessageBodyTemplate" (ngModelChange)="step.settings.MessageBodyTemplate = $event; stepChange.emit(step)" />
                </div>
            </div>
            <div class="col s12">
                <div>
                    <input type="checkbox" [ngModel]="step.settings.IncludePreviousFormResponses" (ngModelChange)="step.settings.IncludePreviousFormResponses = $event; stepChange.emit(step)" /> Include previous form responses
                </div>
            </div>
        </div>
        <div class="row" *ngSwitchCase="2">
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
        <div class="row" *ngSwitchCase="3">
            <d3s-workflow-step-form-editor [step]="step" (stepChange)="step = $event; stepChange.emit(step)"></d3s-workflow-step-form-editor>
        </div>
    </div>
    <div *ngIf="step.hasMultipleInputs" class="col s12" style="padding-top: 8px">
        <input type="checkbox" [ngModel]="step.settings.WaitForAllTransitions" (ngModelChange)="step.settings.WaitForAllTransitions = $event; stepChange.emit(step)" /> Wait for all transitions to complete?
    </div>
</div>
`
})

export class WorkflowStepEditorComponent extends BaseComponent implements OnInit {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() step: NodeModel;
    @Output() stepChange = new EventEmitter();

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

    constructor() {
        super();
    }

    ngOnInit() {
        if (this.step.settings == null)
            this.step.settings = {};
        this.originalStep = _.cloneDeep(this.step);
    }

}