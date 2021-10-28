import { Component, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges, ViewChild, AfterViewChecked } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import {
    WorkflowChangeType,
    NodeModel,
    WorkflowActivityType,
    WorkflowTaskProcedure,
    EmailTaskRecipientType,
    StepType,
    NodeSettings,
    RelationshipUpdateSettings,
    HTTPRequestSettings,
    FieldUpdateSettings,
    HTTPResponseSettings,
} from '../../../../models/workflow.model';
import { Editor } from 'primeng/editor';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';
import { GroupService } from '../../../../services/group.service';

import * as _ from 'lodash';
import * as go from 'gojs';
import { SelectItem } from 'primeng/api';
import { CompanySettingsService } from '../../../../services/settings.service';

@Component({
    selector: 'd3s-workflow-step-editor',
    providers: [WorkflowService, GroupService],
    templateUrl: './workflow-step-editor.component.html'
})

export class WorkflowStepEditorComponent extends BaseComponent implements OnInit, OnChanges, AfterViewChecked, OnDestroy {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() issueObject: string;
    @Input() ChangeType: WorkflowChangeType; 
    @Input() step: NodeModel;
    @Input() diagram: go.Diagram;
    @Output() stepChange = new EventEmitter();
    @ViewChild('ed', { static: false }) ed: Editor;

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
        { value: '0', label: 'Pending Add' },
        { value: '1', label: 'Active' },
        { value: '2', label: 'Pending Delete' },
        { value: '3', label: 'Deleted' },
    ];

    WorkflowChangeType = WorkflowChangeType;

    private quill;
    private destination = [];
    private groups: SelectItem[] = [];

    private responsibilities = [];
    private intersectType = null;
    private responsibleObject: string;
    private responsibleObjectId: number;
    private isLoadingRes = false;
    private procedures: WorkflowTaskProcedure[] = [];

    private fieldsSub;
    private httpFieldsSub;
    private outputFieldsSub;
    private formFields = [];
    private httpFields = [];
    private outputFields = [];
    private formRelationshipFields = [];
    private formRelationship;

    constructor(
        private groupService: GroupService,
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService) {
        super(settingsService);
    }

    ngOnInit() {
        this.fieldsSub = this.workflowFieldsService.formFields$.subscribe((s) => {
            this.filterFormFields();
        });

        this.httpFieldsSub = this.workflowFieldsService.httpFields$.subscribe((s) => {
            this.filterHttpFields();
        });

        this.outputFieldsSub = this.workflowFieldsService.outputFields$.subscribe((s) => {
            this.filterOutputFields();
        });

        this.workflowService.getEmailTaskRecipientType()
            .subscribe((r) => {
                r.forEach((e) => {
                    if (e.ID < 1)
                        return;
                    else if (e.ID == EmailTaskRecipientType.Followers) {
                        if (this.objectType == 'IntersectType')
                            return false;

                        if (!(this.ChangeType == WorkflowChangeType.Add ||
                            this.ChangeType == WorkflowChangeType.Update ||
                            this.ChangeType == WorkflowChangeType.Schedule ||
                            this.ChangeType == WorkflowChangeType.RequestCertification))
                            return;

                        if ((this.ChangeType == WorkflowChangeType.Update) &&
                            !(this.objectType == 'ArtifactType' || this.objectType == 'PolicyType' || this.objectType == 'RuleType' || this.objectType == 'TaxonomyType'))
                            return;

                        if ((this.ChangeType == WorkflowChangeType.Add) && !(this.objectType == 'IssueType'))
                            return;

                        if ((this.ChangeType == WorkflowChangeType.Add) && (this.objectType == 'IssueType')) {
                            if (this.issueObject != null && this.issueObject != '') {
                                let objArr = this.issueObject.split("|", 1);
                                let Issobj = "";
                                if (objArr.length <= 0)
                                    Issobj = " ";
                                else
                                    Issobj = objArr[0];

                                if (!(Issobj == 'ArtifactType' || Issobj == 'PolicyType' || Issobj == 'RuleType' || Issobj == 'TaxonomyType'))
                                    return;
                            }
                        }
                    }
                    else if (e.ID == EmailTaskRecipientType.Initiator) {
                        if (this.ChangeType == WorkflowChangeType.ScoreUpdate)
                            return;
                    }

                    if (e.ID == EmailTaskRecipientType.Initiator && this.ChangeType == WorkflowChangeType.Schedule)
                        return;

                    this.destination.push({
                        value: EmailTaskRecipientType[e.ID],
                        label: e.Name
                    });
                });
            });

        this.groupService.getGroups().subscribe(GroupList => {
            this.groups = GroupList.items.map(g => { return { value: g.Uid, label: g.Name } });
            if (this.step.settings.MessageToGroup != undefined) {
                if (!this.groups.find(g => g.value == this.step.settings.MessageToGroup)) {
                    this.groups.push(<SelectItem>{ value: this.step.settings.MessageToGroup, label: '<invalid group>' });
                }
            }
        });
    }

    ngOnChanges() {
        if (this.step.settings == null)
            this.step.settings = new NodeSettings();
        this.originalStep = _.cloneDeep(this.step);


        if (this.step.activityType == WorkflowActivityType.EmailNotification) {
            if (this.step.settings.SendToDefaultUsers == null) {
                this.step.settings.SendToDefaultUsers = true;                   
            } else {
                this.step.settings.SendToDefaultUsers = this.step.settings.SendToDefaultUsers.toString().toLowerCase() === 'true' ? true : false;
            } 
        } else if (this.step.activityType == WorkflowActivityType.Procedure) {
            this.workflowService.getWorkflowProcedures()
                .subscribe(r => {
                    this.procedures = r;
                });
        } else if (this.step.activityType == WorkflowActivityType.FieldChange) {
            if (this.step.settings.FieldUpdate == null) {
                this.step.settings.FieldUpdate = new FieldUpdateSettings();
            }
               
            if (this.step.settings.FieldUpdate.Field == null) {
                this.step.settings.FieldUpdate.Field = [];
            }

            this.filterFormFields();
            this.filterOutputFields();

        }
        else if (this.step.activityType == WorkflowActivityType.HTTPRequest) {
            if (this.step.settings.HTTPRequest == null) {
                this.step.settings.HTTPRequest = new HTTPRequestSettings();
            }
            if (this.step.settings.HTTPRequest.Timeout == null) {
                this.step.settings.HTTPRequest.Timeout = 90;
            }
            if (this.step.settings.HTTPRequest.Headers == null) {
                this.step.settings.HTTPRequest.Headers = [];
            }

            if (this.step.settings.HTTPRequest.lookupFieldsPassedByValue == null) {
                this.step.settings.HTTPRequest.lookupFieldsPassedByValue = false;
            }
            else {
                this.step.settings.HTTPRequest.lookupFieldsPassedByValue = this.step.settings.HTTPRequest.lookupFieldsPassedByValue.toString().toLowerCase() === "true" ? true : false;
            }

            this.workflowFieldsService.pushHttpFields(this.step);
            this.filterHttpFields();

        }
        else if (this.step.activityType == WorkflowActivityType.HTTPResponse) {
            if (this.step.settings.HTTPResponse == null) {
                this.step.settings.HTTPResponse = new HTTPResponseSettings();
            }
            this.filterOutputFields();
        }
        else if (this.step.activityType == WorkflowActivityType.RelationshipUpdate) {
            if (this.step.settings.RelationshipUpdate == null)
                this.step.settings.RelationshipUpdate = new RelationshipUpdateSettings();
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
                
        if (this.step.settings.WaitForAllTransitions == null)
            this.step.settings.WaitForAllTransitions = false;
        else
            this.step.settings.WaitForAllTransitions = this.step.settings.WaitForAllTransitions.toString().toLowerCase() === 'true' ? true : false;
    }

    ngAfterViewChecked() {
        if (this.ed != null && this.ed.quill != null)
            this.quill = this.ed.quill;
    }

    ngOnDestroy() {
        this.quill = null;
        this.ed = null;

        if (this.fieldsSub) {
            this.fieldsSub.unsubscribe();
        }
        if (this.httpFieldsSub) {
            this.httpFieldsSub.unsubscribe();
        }
        if (this.outputFieldsSub) {
            this.outputFieldsSub.unsubscribe();
        }
    }

    appendField(e: string) {
        if (this.ed != null && this.ed.quill != null)
            this.quill = this.ed.quill;

        if (this.quill != null) {
            let pos = this.quill.getSelection(true);
            let len = pos.index || this.quill.getLength();
            this.quill.insertText(len > 0 ? len - 1 : 0, e, 'api');

            //manually set the html in the model
            this.step.settings.MessageBodyTemplate = this.quill.container.querySelector('.ql-editor').innerHTML;
             
        } else {
            this.step.settings.MessageBodyTemplate =
                ((this.step.settings.MessageBodyTemplate == null) ? '' :
                    this.step.settings.MessageBodyTemplate)
                + e;
        }
        this.stepChange.emit(this.step);
        
    }

    filterFormFields() {
        this.formFields = [];

        this.formRelationshipFields = [];
        if (this.diagram == null) return;

        let fields = this.workflowFieldsService.getFields();

        let upstreamSteps = [];
        this.traverseDiagram(this.step.key, upstreamSteps);
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

        this.stepChange.emit(this.step);
    }

    filterHttpFields() {
        this.httpFields = [];
        let fields = this.workflowFieldsService.getHttpFields();
        let upstreamSteps = [];
        this.traverseDiagram(this.step.key, upstreamSteps);

        fields.forEach(f => {
            let k = upstreamSteps.filter(u => u == f['@stepId']);
            if (k != null && k.length > 0) {
                f['@FormFieldId'] = f['@id'] + '|' + f['@stepId'];
                f['@FormLabel'] = 'HTTP Request :: ' + f['@label'];
                this.httpFields.push(f);
            }
        });

        this.stepChange.emit(this.step);
    }

    filterOutputFields() {
        this.outputFields = [];
        let fields = this.workflowFieldsService.getOutputFields();
        let upstreamSteps = [];
        this.traverseDiagram(this.step.key, upstreamSteps);

        fields.forEach(f => {
            let k = upstreamSteps.filter(u => u == f.StepId);
            if (k != null && k.length > 0) {
                f['@FormFieldId'] = f.Id + '|' + f.StepId;
                f['@FormLabel'] = 'HTTP Response :: ' + f.Name;
                this.outputFields.push(f);
            }
        });

        this.stepChange.emit(this.step);
    }

    traverseDiagram(key: any, upstreamSteps: any[]) {
        let steps = <any[]>this.diagram.model.nodeDataArray;
        let links = <any[]>(<go.GraphLinksModel>this.diagram.model).linkDataArray;

        let step = steps.find(s => s.key == key);
       let toLinks = links.filter(l => l.to == key);

        if (_.includes(upstreamSteps, key)) return;
        upstreamSteps.push(step.key);
       
        if (toLinks == null || toLinks.length < 1) return;

        toLinks.forEach(l => this.traverseDiagram(l.from, upstreamSteps));
        
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

    changeName(e: any) {
        this.step.name = e;
        this.stepChange.emit(this.step);
    }
}