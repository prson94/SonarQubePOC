import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, ViewChild, AfterViewChecked  } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import {
    WorkflowEventRegistration,
    WorkflowObjectType,
    WorkflowChangeType,
    ChangeTypeInfo,
    EventCondition,
    WorkflowListItem,
    WorkflowDiagramModel,
    EmailTaskRecipientType,
} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header, Editor } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../services/workflow-fields.service';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';

import * as _ from 'lodash';
import { State } from '../../../models/asset.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-admin-workflow-editor',
    providers: [WorkflowService, ResponsibilityTypeService],
    templateUrl: './admin-workflow-editor.component.html'
})

export class AdminWorkflowEditorComponent extends BaseComponent implements OnInit, OnDestroy, AfterViewChecked {
    @Input() id: number = 0;
    @Input() model: WorkflowDiagramModel;
    @Output() onClose = new EventEmitter();
    @Output() onSave = new EventEmitter();
    @ViewChild('ed2') ed: Editor;
    @Input() isClone: boolean = false;
    @Output() onWorkflowNameChange = new EventEmitter();
    private workflowObjectTypes: WorkflowObjectType[] = [];
    private defaultWorkflowObject = new WorkflowObjectType();
    private changesTypes: ChangeTypeInfo[] = [];
    private selectedObjectType: any = null;
    private conditions: any[] = [];
    private issueObjectTypes: any[] = [];
    private selectedIssueObjectType: any = null;

    private showAddCondition: boolean = false;

    private SCHEDULE_OBJECT_LIMIT = 2000;
    private isValid = false;
    private errorMessage = "";
    private warningMessage = "";
    private hideShoppingCart = true;
    
    WorkflowChangeType = WorkflowChangeType;
    EmailTaskRecipientType = EmailTaskRecipientType;

    private destination = [];
    private responsibilities = [];

    private quill: any;
    private satisfyAllConditions: boolean = true;

    constructor(
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService,
        private responsibilityService: ResponsibilityTypeService) {
        super();
    }

    ngOnInit() {
        if (CompanySettings != null && CompanySettings.EnableShoppingCart != null && CompanySettings.EnableShoppingCart.toString() == 'true') {
            this.hideShoppingCart = false;
        }

        this.load()
            .then(() => this.model.Event.SettingsObject.Settings.MessageRecipientType = 'SpecificUser')
            .then(() => this.isLoading = false)
    }

    ngAfterViewChecked() {
        //always try to use the quill API if possible. Avoids inserting extra <p> tags and inserting on a newline
        if (this.ed != null && this.ed.quill != null) {
            this.quill = this.ed.quill;
        }
    }

    ngOnDestroy() {
        this.quill = null;
        this.ed = null;
    }

    load(): Promise<any> {
        
        this.isLoading = true;

        return this.workflowService.getChangeTypes()
            .then(r => { this.changesTypes = r; })
            .then(() => {

                return this.workflowService.getWorkflowTypeModel(this.id)
                    .then(r => {
                        if (this.id > 0 && this.model == null && r != null)
                            this.model = r;

                        //create initial model and settings if needed
                        if (this.model == null)
                            this.model = new WorkflowDiagramModel();
                        if (this.model.Event.SettingsObject == null)
                            this.model.Event.SettingsObject = {};
                        if (this.model.Event.SettingsObject.Settings == null)
                            this.model.Event.SettingsObject.Settings = {};

                        if (this.model.Event.SettingsObject != null && this.model.Event.SettingsObject.Settings != null
                            && this.model.Event.SettingsObject.Settings.Visible != null) {
                            if (this.model.Event.SettingsObject.Settings.SendAggregateEmail != null)
                                //convert to bool
                                this.model.Event.SettingsObject.Settings.SendAggregateEmail = this.model.Event.SettingsObject.Settings.SendAggregateEmail.toString().toLowerCase() == "true" ? true : false;
                        }

                        this.selectedObjectType = (this.model.Event.ObjectID != null) ? this.model.Event.Object + '|' + this.model.Event.ObjectID.toString() : '';
                        this.objectID = this.model.Event.ObjectID;
                        this.objectType = this.model.Event.Object;


                        if ((this.model.Event.ConditionObject == null || _.isEmpty(this.model.Event.ConditionObject)) && this.model.Event.Condition != null && this.model.Event.Condition.toString() === this.model.Event.Condition && this.model.Event.Condition.startsWith('{')) {
                            let conditions = JSON.parse(this.model.Event.Condition).Conditions.Condition;
                            this.conditions = [];
                            conditions.forEach(c => this.conditions.push(c));
                        }
                        else if (this.model.Event.ConditionObject != null && this.model.Event.ConditionObject.Condition != null) {
                            this.conditions = [];
                            if (this.model.Event.ConditionObject.Condition.length == null)
                                this.conditions.push(this.model.Event.ConditionObject.Condition);
                            else
                                this.conditions = this.model.Event.ConditionObject.Condition;
                        }
                    })
                    .then(() => this.workflowService.getWorkflowFieldTypes(this.objectID, this.objectType, true))
                    .then(r => {
                        //need to apply names to loaded conditions
                        r.forEach(t => {
                            let c = this.conditions.filter(c => c['@FieldTypeID'] == t.ID);
                            if (c != null)
                                c.forEach(f => f['@FieldName'] = t.FriendlyName);
                        });
                    })
                    .then(() => {
                        //apply names to contextual fields
                        this.conditions.filter(c => c['@ContextualFieldID'] != null).forEach(c => {
                            let cx = this.workflowFieldsService
                                .getContextualFieldsForType(this.model.Event.ChangeType, this.model.Event.Object)
                                .find(x => x.value == 'Contextual|' + c['@ContextualFieldID']);
                            if (cx != null)
                                c['@FieldName'] = cx.label;
                        });
                    })
                    .then(() => this.workflowService.getWorkflowObjectTypes(this.model.Event.ChangeType))
                    .then(r => this.workflowObjectTypes = [this.defaultWorkflowObject].concat(r))
                    .then(() => {
                        if (this.hideShoppingCart) {
                            this.workflowObjectTypes = this.workflowObjectTypes.filter(w => w.type != 'ShoppingCartType');
                        }
                    })
                    .then(() => {
                        if (this.objectType == 'IssueType') {
                            this.issueObjectTypes = this.workflowObjectTypes.slice().filter(w => w.type != 'IssueType');

                            let objectIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'IssueObject');
                            let objectIdIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'IssueObjectID');

                            if (objectIndex > -1 && objectIdIndex > -1) {
                                this.selectedIssueObjectType = this.conditions[objectIndex]['@Value'] + '|' + this.conditions[objectIdIndex]['@Value'];
                            }

                        }
                    });
            })
            .then(() => this.loadResponsibilities())
            .then(() => { this.validate(); });


    }

    loadObjects() {
        return this.workflowService.getWorkflowObjectTypes(this.model.Event.ChangeType)
            .then(r => this.workflowObjectTypes = [this.defaultWorkflowObject].concat(r))
            .then(() => {
                if (this.hideShoppingCart) {
                    this.workflowObjectTypes = this.workflowObjectTypes.filter(w => w.type != 'ShoppingCartType');
                }
            });
    }

    selectObjectType(e: any) {
        this.selectedObjectType = e;
        this.showAddCondition = false;
        this.conditions = [];

        if (e != null && e.indexOf('|') > -1) {
            this.objectType = e.split('|')[0];
            this.objectID = +e.split('|')[1];

            if (this.model.Event.SettingsObject.Settings.TaxonomyTypeID != null && this.objectType != 'ArtifactType') {
                delete this.model.Event.SettingsObject.Settings.TaxonomyTypeID;
            }

            if (this.model.Event.ChangeType != WorkflowChangeType.Schedule
                && this.model.Event.SettingsObject.Settings.ScheduleInterval != null) {
                delete this.model.Event.SettingsObject.Settings.ScheduleInterval;
            }

            if (this.objectType == 'IssueType') {
                this.issueObjectTypes = this.workflowObjectTypes.slice().filter(w => w.type != 'IssueType');
            }

            this.loadResponsibilities().then(() => this.validate());
        } else {
            this.isValid = false;
        }
    }

    loadResponsibilities(): Promise<any> {
        if (this.objectType == null || this.objectID == null) {
            this.responsibilities = [];
            return Promise.resolve();
        }
        return this.responsibilityService.getResponsibilityTypesByObject(this.objectType, this.objectID)
            .then(r => this.responsibilities = r);
    }

    showCondition() {
        if (this.showAddCondition)
            return;
        this.showAddCondition = true;
    }

    addCondition(e: any) {
        this.conditions.push(e);
        this.conditions = this.conditions.slice();
        this.showAddCondition = false;
        this.validate();
    }

    hasPendingWorkflowItems() {
        this.workflowService.hasPendingWorkflowItems(this.model.Type.ID)
            .then(x => {
                if (x) {
                    this.warningMessage = "There are pending workflow items for this workflow. These items can still be completed, but no new workflow items will be created.";
                }
            });
    }

    onStateChange($event) {
        if ($event) {
            this.model.Type.State = State.Active;
            this.warningMessage = "";
        } else {
            this.model.Type.State = State.InActive;
            if (this.model.Type.ID != 0) this.hasPendingWorkflowItems();
        }
    }

    applyConnectorToConditions($event) {
        this.conditions.forEach(function (condition) {
            condition['@Connector'] = $event ? 'AND' : 'OR';
        }.bind(this));
        this.validate();
    }

    remove(item: any) {
        let i = this.conditions.findIndex(c => c == item);
        this.conditions.splice(i, 1);
        this.conditions = this.conditions.slice();
        this.validate();
    }

    save() {
        this.model.Event.conditions = this.conditions;
        this.model.Event.Object = this.objectType;
        this.model.Event.ObjectID = this.objectID;

        this.model.Type.PublishedVersionID = null;

        this.conditions.forEach(c => {
            delete c['@FieldName'];
        });

        let objectIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'IssueObject');
        let objectIdIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'IssueObjectID');

        if (this.objectType == 'IssueType') {
            if (this.selectedIssueObjectType != null && this.selectedIssueObjectType.indexOf('|') > -1) {
                let obj = this.selectedIssueObjectType.split('|')[0];
                let objid = this.selectedIssueObjectType.split('|')[1];

                if (objectIndex < 0) {
                    this.conditions.push({
                        '@ContextualFieldID': 'IssueObject',
                        '@Operator': '=',
                        '@ValueType': 'T',
                        '@Value': obj
                    });
                } else {
                    this.conditions[objectIndex]['@Value'] = obj;
                }

                if (objectIdIndex < 0) {
                    this.conditions.push({
                        '@ContextualFieldID': 'IssueObjectID',
                        '@Operator': '=',
                        '@ValueType': 'D',
                        '@Value': objid
                    });
                } else {
                    this.conditions[objectIdIndex]['@Value'] = objid;
                }
            } else {
                if (objectIndex > -1) {
                    this.conditions.splice(objectIndex, 1);
                }

                if (objectIdIndex > -1) {
                    objectIdIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'IssueObjectID'); //index may have changed depending on order
                    this.conditions.splice(objectIdIndex, 1);
                }
            }
        } else {
            if (objectIndex > -1) {
                this.conditions.splice(objectIndex, 1);
            }

            if (objectIdIndex > -1) {
                objectIdIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'IssueObjectID');
                this.conditions.splice(objectIdIndex, 1);
            }
        }

        if (this.model.Event.SettingsObject.Settings.SendAggregateEmail == false
            || this.model.Event.SettingsObject.Settings.SendAggregateEmail == null) {
            //delete aggregate email settings
            delete this.model.Event.SettingsObject.Settings.MessageSubjectTemplate;
            delete this.model.Event.SettingsObject.Settings.MessageRecipientType;
            delete this.model.Event.SettingsObject.Settings.MessageToUser;
            delete this.model.Event.SettingsObject.Settings.ResponsibilityTypeID;
            delete this.model.Event.SettingsObject.Settings.MessageBodyTemplate;

            if (this.model.Event.ChangeType != WorkflowChangeType.Schedule) {
                delete this.model.Event.SettingsObject.Settings.SendAggregateEmail;
            }
        }

        this.model.Event.Condition = JSON.stringify({ Conditions: { Condition: this.conditions } });
        this.model.Event.Settings = JSON.stringify(this.model.Event.SettingsObject);

        this.onSave.emit(this.model);
    }

    validate() {
        this.errorMessage = "";

        if (this.model == null) return;

        this.conditions.forEach(c => {
            if (c['@ContextualFieldID'] != null && (c['@ContextualFieldID'] == 'IssueObject' || c['@ContextualFieldID'] == 'IssueObjectID'))
                return;

            if (c['@FieldName'] == null) {
                this.errorMessage = "One or more conditions is not valid.";
                this.isValid = false;
                return;
            }
        });

        if (this.model.Event.ChangeType == WorkflowChangeType.Schedule && this.selectedObjectType != '' && this.selectedObjectType != null) {
            if (this.model.Event.SettingsObject.Settings.ScheduleInterval == null) {
                this.errorMessage = "Please enter a run interval";
                this.isValid = false;
                return;
            } else if (+this.model.Event.SettingsObject.Settings.ScheduleInterval < 1) {
                this.errorMessage = "Run interval must be greater than or equal to 1";
                this.isValid = false;
                return;
            }

            if (this.conditions.length < 1) {
                this.errorMessage = "At least 1 condition is required when using change type Schedule.";
                this.isValid = false;
                return;
            }

            let t = this.workflowObjectTypes.find(t => t.value == this.selectedObjectType);

            if (t != null && t.count > this.SCHEDULE_OBJECT_LIMIT) {
                this.errorMessage = `The chosen object type has more than ${this.SCHEDULE_OBJECT_LIMIT} items, which exceeds the limit for change type Schedule.`;
                this.isValid = false;
                return;
            }
        }

        if (this.model.Event.SettingsObject.Settings.SendAggregateEmail != null
            && this.model.Event.SettingsObject.Settings.SendAggregateEmail.toString() == 'true') {

            if (this.model.Event.SettingsObject.Settings.MessageSubjectTemplate == null ||
                this.model.Event.SettingsObject.Settings.MessageSubjectTemplate == '') {
                this.isValid = false;
                return;
            }

            if (this.model.Event.SettingsObject.Settings.MessageRecipientType == null ||
                this.model.Event.SettingsObject.Settings.MessageRecipientType == '') {
                this.isValid = false;
                return;
            } else {
                if (this.model.Event.SettingsObject.Settings.MessageRecipientType == 'Responsibility') {
                    if (this.model.Event.SettingsObject.Settings.ResponsibilityTypeID == null ||
                        this.model.Event.SettingsObject.Settings.ResponsibilityTypeID < 1) {
                        this.isValid = false;
                        return;
                    }
                } else if (this.model.Event.SettingsObject.Settings.MessageRecipientType == 'SpecificUser') {
                    if (this.model.Event.SettingsObject.Settings.MessageToUser == null ||
                        this.model.Event.SettingsObject.Settings.MessageToUser == '') {
                        this.isValid = false;
                        return;
                    }
                }
            }
        }

        if (this.model.Type.Name == null || this.model.Type.Name == '') {
            this.isValid = false;
            return;
        }

        if (this.model.Event.ChangeType == null || this.model.Event.ChangeType.toString() == '') {
            this.isValid = false;
            return;
        }

        if (this.selectedObjectType == null || this.selectedObjectType == '') {
            this.isValid = false;
            return;
        }

        this.isValid = true;
    }

    appendField(e: string) {
        if (this.ed != null && this.ed.quill != null)
            this.quill = this.ed.quill;

        if (this.quill != null) {
            let pos = this.quill.getSelection(true);
            let len = pos.index || this.quill.getLength();
            this.quill.insertText(len < 1 ? 0 : len - 1, e, 'api');
        } else { //fallback in case quill API is null for whatever reason
            this.model.Event.SettingsObject.Settings.MessageBodyTemplate =
                ((this.model.Event.SettingsObject.Settings.MessageBodyTemplate == null) ? ' '
                    : this.model.Event.SettingsObject.Settings.MessageBodyTemplate)
                + e;
        }
    }
}
