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
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';
import { ResponsibilityTypeService } from '../../../../services/responsibility-type.service';

import * as _ from 'lodash';
import * as go from 'gojs';

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
    @Input() diagram: go.Diagram;
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

    private fieldsSub;
    private formFields = [];

    constructor(private responsibilityService: ResponsibilityTypeService, private workflowService: WorkflowService, private workflowFieldsService: WorkflowFieldsService) {
        super();
    }

    ngOnInit() {
        this.fieldsSub = this.workflowFieldsService.formFields$.subscribe(s => {
            this.filterFormFields();
        });

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

            this.filterFormFields();

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
        this.fieldsSub.unsubscribe();
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

    filterFormFields() {
        this.formFields = [];
        if (this.diagram == null) return;

        let fields = this.workflowFieldsService.getFields();

        let upstreamSteps = [];
        this.traverseDiagram(this.step.key, upstreamSteps);
        console.log('upstreamSteps',upstreamSteps);
        fields.forEach(f => {
            let k = upstreamSteps.filter(u => u == f['@stepId']);
            if (k != null && k.length > 0) {
                f['@FieldName'] = 'Form :: ' + f['@label'];
                this.formFields.push(f);
            }
        });
    }

    traverseDiagram(key: any, upstreamSteps: any[]) {
        let steps = <any[]>this.diagram.model.nodeDataArray;
        let links = <any[]>(<go.GraphLinksModel>this.diagram.model).linkDataArray;

        let step = steps.find(s => s.key == key);
        let toLinks = links.filter(l => l.to == key);

        upstreamSteps.push(step.key);

        if (toLinks == null || toLinks.length < 1) return;

        toLinks.forEach(l => this.traverseDiagram(l.from, upstreamSteps));
        
    }
}