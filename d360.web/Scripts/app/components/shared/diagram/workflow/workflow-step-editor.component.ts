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
    EmailTaskRecipientType,
    StepType,
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
    @Input() step: NodeModel;
    @Input() diagram: go.Diagram;
    @Output() stepChange = new EventEmitter();
    @ViewChild('ed') ed: Editor;

    WorkflowActivityType = WorkflowActivityType;
    EmailTaskRecipientType = EmailTaskRecipientType;
    StepType = StepType;

    private originalStep: NodeModel;
    private status = [
        'Draft',
        'Under Review',
        'Certified'
    ];
    private states = [
        //'Unknown',
        { value: '0', label: 'Pending Add' },
        { value: '1', label: 'Active' },
        { value: '2', label: 'Pending Delete' },
        { value: '3', label: 'Deleted' },
    ];
    
    private quill;
    private destination = [];

    private responsibilities = [];
    private procedures: WorkflowTaskProcedure[] = [];

    private fieldsSub;
    private formFields = [];
    private formRelationshipFields = [];
    private formRelationship;

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

        } else if (this.step.activityType == WorkflowActivityType.RelationshipUpdate) {
            if (this.step.settings.RelationshipUpdate == null)
                this.step.settings.RelationshipUpdate = {};
            if (this.step.settings.RelationshipUpdate.Relationship == null)
                this.step.settings.RelationshipUpdate.Relationship = {};

            this.filterFormFields();

            if (this.step.settings.RelationshipUpdate.Relationship['@FormFieldId'] != null && this.step.settings.RelationshipUpdate.Relationship['@FormStepId'] != null) {
                this.formRelationship = this.step.settings.RelationshipUpdate.Relationship['@FormFieldId'] + '|' + this.step.settings.RelationshipUpdate.Relationship['@FormStepId'];
            }

            if (this.step.settings.RelationshipUpdate.Relationship['@AppendValue'] != null) {
                this.step.settings.RelationshipUpdate.Relationship['@AppendValue'] = (this.step.settings.RelationshipUpdate.Relationship['@AppendValue'].toString().toLowerCase() == 'true')
            }
            if (this.step.settings.RelationshipUpdate.Relationship['@ClearValue'] != null) {
                this.step.settings.RelationshipUpdate.Relationship['@ClearValue'] = (this.step.settings.RelationshipUpdate.Relationship['@ClearValue'].toString().toLowerCase() == 'true')
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
        this.formRelationshipFields = [];
        if (this.diagram == null) return;

        let fields = this.workflowFieldsService.getFields();

        let upstreamSteps = [];
        this.traverseDiagram(this.step.key, upstreamSteps);
        //console.log('upstreamSteps',upstreamSteps, fields);
        fields.forEach(f => {
            let k = upstreamSteps.filter(u => u == f['@stepId']);
            if (k != null && k.length > 0) {
                f['@FormFieldId'] = f['@id'] + '|' + f['@stepId'];
                f['@FormLabel'] = 'Form :: ' + f['@label'];
                this.formFields.push(f);
                if (f['@type'] == 'relationshipType') {
                    this.formRelationshipFields.push(f);
                }
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

    changeResponsiblity(e: any) {
        this.step.settings.ResponsibilityTypeID = e;
        let rt = this.responsibilities.find(r => r.ResponsibilityTypeID == e);
        if (rt)
            this.step.settings.ResponsiblityTypeName = rt.Name;
        this.stepChange.emit(this.step)
    }

    changeRelationship(e: any) {
        this.formRelationship = e;
        if (e == null || e.indexOf('|') < 0) {
            this.step.settings.RelationshipUpdate.Relationship['@FormFieldId'] = null;
            this.step.settings.RelationshipUpdate.Relationship['@FormStepId'] = null;
        } else {
            let vals = this.formRelationship.split('|');
            this.step.settings.RelationshipUpdate.Relationship['@FormFieldId'] = vals[0];
            this.step.settings.RelationshipUpdate.Relationship['@FormStepId'] = vals[1];
        }

        this.stepChange.emit(this.step);
    }

    changeValueType(e: any, field: string) {
        this.step.settings.RelationshipUpdate.Relationship[field] = e;
        if (field == '@AppendValue' && e == true) {
            this.step.settings.RelationshipUpdate.Relationship['@ClearValue'] = false;
        } else if (field == '@ClearValue' && e == true) {
            this.step.settings.RelationshipUpdate.Relationship['@AppendValue'] = false;
        }
        this.stepChange.emit(this.step);
    }
}