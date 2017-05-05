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
    EmailTaskRecipientType,
} from '../../../models/workflow.model';
import { Taxonomy } from '../../../models/taxonomy.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../services/workflow-fields.service';
import { TaxonomiesService } from '../../../services/taxonomies.service';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';

declare var CompanySettings;

@Component({
    selector: 'd3s-admin-workflow-new-editor',
    providers: [WorkflowService, TaxonomiesService, ResponsibilityTypeService],
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
    private conditions: any[] = [];

    private showAddCondition: boolean = false;
    private objectType: string;
    private objectId: number;
    private saveButtonText: string = 'Next';
    private hideObject: boolean = false;

    private subjectAreaName: string;
    private taxonomies: Taxonomy[] = [];

    private arbitraryScheduleObjectLimit = 2000;
    private isValid = false;
    private errorMessage = "";

    WorkflowChangeType = WorkflowChangeType;
    EmailTaskRecipientType = EmailTaskRecipientType;

    private destination = [];

    private responsibilities = [];

    constructor(
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService,
        private taxonomyService: TaxonomiesService,
        private responsibilityService: ResponsibilityTypeService) {
        super();
    }

    ngOnInit() {

        if (CompanySettings.ArtifactType_TaxonomyTypeID != null && CompanySettings.ArtifactType_TaxonomyTypeID != '') {
            this.subjectAreaName = CompanySettings.ArtifactType_TaxonomyTypeID;
        } else {
            this.subjectAreaName = 'Subject Area';
        }

        this.load()
            .then(() => this.workflowService.getEmailTaskRecipientType())
            .then(r => {
                r.forEach(e => {
                    if (e.ID < 1)
                        return;
                    this.destination.push({
                        value: EmailTaskRecipientType[e.ID],
                        label: e.Name
                    });
                });
            })
            .then(() => {
            //create initial model and settings if needed
            if (this.model == null)
                this.model = new WorkflowDiagramModel();
            if (this.model.Event.SettingsObject == null)
                this.model.Event.SettingsObject = {};
            if (this.model.Event.SettingsObject.Settings == null)
                this.model.Event.SettingsObject.Settings = {};
            this.isLoading = false;
        });



    }

    load(): Promise<any> {
        this.isLoading = true;

        return this.workflowService.getWorkflowObjectTypes()
            .then(r => { this.workflowObjectTypes = r; })
            .then(() => this.workflowService.getChangeTypes())
            .then(r => { this.changesTypes = r; })
            .then(() => this.responsibilityService.getResponsibilityTypes())
            .then(r => { this.responsibilities = r; })
            .then(() => {
                if (this.id < 1) {
                    this.saveButtonText = 'Next';
                    return;
                } else {
                    this.saveButtonText = 'Save';
                    return this.workflowService.getWorkflowTypeModel(this.id)
                        .then(r => {
                            this.model = r

                            if (this.model.Event.SettingsObject != null && this.model.Event.SettingsObject.Settings != null) {
                                this.hideObject = (this.model.Event.SettingsObject.Settings.Visible == "false") ? true : false;

                                if (this.model.Event.SettingsObject.Settings.SendAggregateEmail != null)
                                    //convert to bool
                                    this.model.Event.SettingsObject.Settings.SendAggregateEmail = this.model.Event.SettingsObject.Settings.SendAggregateEmail.toString().toLowerCase() == "true" ? true : false;
                            }

                            this.selectedObjectType = this.model.Event.Object + '|' + this.model.Event.ObjectID.toString();
                            this.objectId = this.model.Event.ObjectID;
                            this.objectType = this.model.Event.Object;

                            if (this.objectType == 'ArtifactType')
                                this.loadTaxonomies();

                            console.log(r);

                            if (this.model.Event.ConditionObject != null) {
                                this.conditions = [];

                                if (this.model.Event.ConditionObject.Condition.length == null)
                                    this.conditions.push(this.model.Event.ConditionObject.Condition);
                                else
                                    this.conditions = this.model.Event.ConditionObject.Condition;
                            }
                        })
                        .then(() => this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType))
                        .then(r => {
                            //need to apply names to loaded conditions
                            r.forEach(t => {
                                let c = this.conditions.find(c => c['@FieldTypeID'] == t.ID);
                                if (c != null)
                                    c['@FieldName'] = t.FriendlyName;
                            });
                        })
                        .then(() => {
                            //apply names to contextual fields
                            this.conditions.filter(c => c['@ContextualFieldID'] != null).forEach(c => {
                                let cx = this.workflowFieldsService
                                    .getContextualFieldsForType(this.model.Event.ChangeType)
                                    .find(x => x.value == 'Contextual|' + c['@ContextualFieldID']);
                                if (cx != null)
                                    c['@FieldName'] = cx.label;
                            });
                        });
                }
            })
            .then(() => { this.validate(); });

    }

    selectObjectType(e: any) {
        this.selectedObjectType = e;
        this.showAddCondition = false;
        this.conditions = [];

        if (e.indexOf('|') < 0)
            return;

        this.objectType = e.split('|')[0];
        this.objectId = +e.split('|')[1];

        if (this.objectType == 'ArtifactType')
            this.loadTaxonomies();
        else if (this.model.Event.SettingsObject.Settings.TaxonomyTypeID != null) {
            delete this.model.Event.SettingsObject.Settings.TaxonomyTypeID;
        }

        if (this.model.Event.ChangeType != WorkflowChangeType.Schedule
            && this.model.Event.SettingsObject.Settings.ScheduleInterval != null) {
            delete this.model.Event.SettingsObject.Settings.ScheduleInterval;
        }

        let type = this.workflowObjectTypes.find(f => f.value == e);
        if (type != null && type.count > this.arbitraryScheduleObjectLimit) {
            if (this.model.Event.ChangeType == WorkflowChangeType.Schedule)
                this.model.Event.ChangeType = null;
        }
        this.validate();

    }

    loadTaxonomies(): Promise<any> {
        return this.taxonomyService.getTaxonomies()
            .then(r => this.taxonomies = r);
    }

    showCondition() {
        if (this.showAddCondition)
            return;
        this.showAddCondition = true;
    }

    addCondition(e: any) {
        this.conditions.push(e);
        this.showAddCondition = false;
        this.validate();
        //console.log(this.conditions);
    }

    remove(item: any) {
        let i = this.conditions.findIndex(c => c == item);
        this.conditions.splice(i, 1);
        this.validate();
    }

    save() {
        this.model.Event.SettingsObject.Settings.Visible = !this.hideObject;

        this.model.Event.conditions = this.conditions;
        this.model.Event.Object = this.objectType;
        this.model.Event.ObjectID = this.objectId;

        this.model.Type.PublishedVersionID = null;

        this.conditions.forEach(c => {
                delete c['@FieldName']; 
        });

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
        this.model.Event.Settings = JSON.stringify( this.model.Event.SettingsObject );

        console.log('save: ', this.model.Event);

        this.isLoading = true;
        this.workflowService.saveWorkflowDiagramModel(this.model)
            .then(r => {
                this.isLoading = false;
                this.model.Type.ID = r;
                this.onSave.emit(this.model);
            });
    }

    validate() {
        this.errorMessage = "";

        if (this.model == null) return;

        if (this.model.Event.ChangeType == WorkflowChangeType.Schedule && this.selectedObjectType != '' && this.selectedObjectType != null) {
            if (this.conditions.length < 1) {
                this.errorMessage = "At least 1 condition is required when using change type Schedule.";
                this.isValid = false;
                return;
            }

            let t = this.workflowObjectTypes.find(t => t.value == this.selectedObjectType);

            if (t != null && t.count > this.arbitraryScheduleObjectLimit) {
                this.errorMessage = `The chosen object type has more than ${this.arbitraryScheduleObjectLimit} items, which exceeds the limit for change type Schedule.`;
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

        if (this.model.Event.ChangeType == null || this.selectedObjectType == null) {
            this.isValid = false;
            return;
        }

        if (this.selectedObjectType == null || this.selectedObjectType == '') {
            this.isValid = false;
            return;
        }

        this.isValid = true;
    }
}