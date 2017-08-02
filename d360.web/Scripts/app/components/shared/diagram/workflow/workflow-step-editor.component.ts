import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
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
    EmailTaskRecipientType
} from '../../../../models/workflow.model';
import { FieldType } from '../../../../models/fields.model';
import { Column, Header, Editor } from 'primeng/primeng';
import { WorkflowService } from '../../../../services/workflow.service';
import { ResponsibilityTypeService } from '../../../../services/responsibility-type.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-step-editor',
    providers: [WorkflowService, ResponsibilityTypeService],
    templateUrl: './workflow-step-editor.component.html'
})

export class WorkflowStepEditorComponent extends BaseComponent implements OnInit, OnChanges, AfterViewChecked, OnDestroy {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() isAggregate: boolean = false;
    @Input() step: NodeModel;
    @Output() stepChange = new EventEmitter();
    @ViewChild('ed') ed: Editor;

    WorkflowActivityType = WorkflowActivityType;
    EmailTaskRecipientType = EmailTaskRecipientType;

    private originalStep: NodeModel;
    private status = [
        'Draft',
        'Under Review',
        'Certified'
    ];

    private quill;
    private destination = [];

    private responsibilities = [];
    private procedures: WorkflowTaskProcedure[] = [];

    constructor(private responsibilityService: ResponsibilityTypeService, private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.workflowService.getEmailTaskRecipientType()
            .then(r => {
                r.forEach(e => {
                    if (e.ID < 1)
                        return;
                    this.destination.push({
                        value: EmailTaskRecipientType[e.ID],
                        label: e.Name
                    });
                });
            });
    }

    ngOnChanges() {
        if (this.step.settings == null)
            this.step.settings = {};
        this.originalStep = _.cloneDeep(this.step);

        if (this.step.activityType == WorkflowActivityType.EmailNotification) {
            this.responsibilityService.getResponsibilityTypesByObject(this.objectType, this.objectId)
                .then(r => {
                    this.responsibilities = r;
                    //console.log(r);
                });
        } else if (this.step.activityType == WorkflowActivityType.Procedure) {
            this.workflowService.getWorkflowProcedures()
                .then(r => {
                    this.procedures = r;
                });
        } else if (this.step.activityType == WorkflowActivityType.FieldChange) {
            if (this.step.settings.FieldUpdate == null) {
                this.step.settings.FieldUpdate = {};
            }
               
            if (this.step.settings.FieldUpdate.Field == null) {
                this.step.settings.FieldUpdate.Field = {};
            }

        }

        if (this.ed != null && this.ed.quill != null)
            this.quill = this.ed.quill;
        else
            this.quill = null;
    }

    ngAfterViewChecked() {
        if (this.ed != null && this.ed.quill != null)
            this.quill = this.ed.quill;
    }

    ngOnDestroy() {
        this.quill = null;
        this.ed = null;

    }

    appendField(e: string) {
        //console.log(this.step.settings.MessageBodyTemplate, this.quill);

        if (this.quill != null) {
            let len = this.quill.getLength();
            this.quill.insertText(len > 0 ? len - 1 : 0, e, 'api');
             
        } else {
            this.step.settings.MessageBodyTemplate =
                ((this.step.settings.MessageBodyTemplate == null) ? '' :
                    this.step.settings.MessageBodyTemplate)
                    + e;
        }
        
    }
}