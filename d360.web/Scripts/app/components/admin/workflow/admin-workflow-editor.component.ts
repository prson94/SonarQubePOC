import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, ViewChild, AfterViewChecked } from '@angular/core';
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
import { Editor } from 'primeng/editor';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../services/workflow-fields.service';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { map, finalize, concatMap } from 'rxjs/operators';
import * as _ from 'lodash';
import { State } from '../../../models/asset.model';
import { of, Subscription } from 'rxjs';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';

@Component({
    selector: 'd3s-admin-workflow-editor',
    providers: [WorkflowService, ResponsibilityTypeService],
    templateUrl: './admin-workflow-editor.component.html'
})

export class AdminWorkflowEditorComponent extends BaseComponent implements OnInit, OnDestroy, AfterViewChecked {
    @Input() id: number = 0;
    @Input() uid: string = "00000000-0000-0000-0000-000000000000";
    @Input() model: WorkflowDiagramModel;
    @Output() onClose = new EventEmitter();
    @Output() onSave = new EventEmitter();
    @ViewChild('ed2', { static: false }) ed: Editor;
    @Input() isClone: boolean = false;
    @Output() onWorkflowNameChange = new EventEmitter();
    private workflowObjectTypes: WorkflowObjectType[] = [];
    private changesTypes: ChangeTypeInfo[] = [];
    private selectedObjectType: any = null;
    private conditions: any[] = [];
    private issueObjectTypes: any[] = [];
    private scoreTypes: any[] = [];
    private scheduleTypes: any[] = [{ label: $localize`Daily`, value: 'd' }, { label: $localize`Hourly`, value: 'h' }]
    private resSub: Subscription;
    private defaultWorkflowObject = new WorkflowObjectType();

    private showAddCondition: boolean = false;

    private SCHEDULE_OBJECT_LIMIT = 2000;
    private isValid = false;
    errorMessage = "";
    warningMessage = "";
    private hideShoppingCart = true;

    WorkflowChangeType = WorkflowChangeType;
    EmailTaskRecipientType = EmailTaskRecipientType;

    private destination = [];
    private responsibilities = [];

    private quill: any;
    private satisfyAllConditions: boolean = true;
    excludedContextualFields = [
        'IssueObject',
        'IssueObjectID',
        'ScoreType'
    ];

    constructor(
        private responsibilityService: ResponsibilityTypeService,
        private messageService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private workflowFieldsService: WorkflowFieldsService,
        private workflowService: WorkflowService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.hideShoppingCart = !this.settingsService.getSettingById(CompanySettingEnum.EnableShoppingCart).BooleanSetting.Value;

        this.defaultWorkflowObject.label = "";
        this.defaultWorkflowObject.value = "";

        this.load();
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

    close() {
        this.isLoading = true;
        if (this.isClone) {
            this.workflowService.deleteWorkflowType(this.id, this.uid)
                .subscribe(r => {
                    this.isLoading = false;
                    this.messageService.showInfoMessage($localize`Workflow not saved.`, "");
                    this.onClose.emit();
                }, err => {
                    this.messageService.showError($localize`Problem deleting cloned Workflow`, err);
                    this.onClose.emit();
                });
        } else {
            this.onClose.emit();
        }
    }

    load() {
        this.isLoading = true;
        this.workflowService.getChangeTypes()
            .pipe(map(r => { this.changesTypes = r; }))
            .pipe(concatMap(() => this.workflowService.getWorkflowTypeModel(this.id, this.uid)
                .pipe(
                    map(r => {
                        if ((this.id > 0 || (this.uid && this.uid != "00000000-0000-0000-0000-000000000000")) && this.model == null && r != null)
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
                        if (this.model.Event.SettingsObject != null && this.model.Event.SettingsObject.Settings != null
                            && this.model.Event.SettingsObject.Settings.ScheduleType == null) {
                            this.model.Event.SettingsObject.Settings.ScheduleType = 'd';
                        }

                        this.selectedObjectType = (this.model.Event.ObjectID != null) ? this.model.Event.Object + '|' + this.model.Event.ObjectID.toString() : '';
                        this.objectID = this.model.Event.ObjectID;
                        this.objectType = this.model.Event.Object;


                        this.workflowFieldsService.setWorkflow(this.model.Event.Object, this.model.Event.ObjectID, this.model.Event.ChangeType);

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
                    }))))
            .pipe(concatMap(() => this.workflowService.getWorkflowObjectTypes(this.model.Event.ChangeType)
                .pipe(
                    map(r => {
                        this.workflowObjectTypes = [this.defaultWorkflowObject].concat(r);
                        if (this.hideShoppingCart) {
                            this.workflowObjectTypes = this.workflowObjectTypes.filter(w => w.type != 'ShoppingCartType');
                        }

                        this.model.Event.IssueObject = '';
                        if (this.objectType == 'IssueType') {
                            this.issueObjectTypes = this.workflowObjectTypes.slice().filter(w => w.type != 'IssueType' && w.type != 'ReferenceItemType');

                            let objectIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'IssueObject');
                            let objectIdIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'IssueObjectID');

                            if (objectIndex > -1 && objectIdIndex > -1) {
                                this.model.Event.IssueObject = this.conditions[objectIndex]['@Value'] + '|' + this.conditions[objectIdIndex]['@Value'];
                            }

                        }

                        let type = this.objectType;
                        let id = this.objectID;

                        if (this.model.Event.IssueObject != null && this.model.Event.IssueObject.indexOf('|') > -1) {
                            type = this.model.Event.IssueObject.split('|')[0];
                            id = +this.model.Event.IssueObject.split('|')[1];
                        }

                        if (this.model.Event.ChangeType == WorkflowChangeType.ScoreUpdate
                            || this.model.Event.ChangeType == WorkflowChangeType.Update
                            || this.model.Event.ChangeType == WorkflowChangeType.RequestCertification
                            || this.model.Event.ChangeType == WorkflowChangeType.Schedule) {
                            this.workflowService.getScoreTypes(id, type)
                                .subscribe(res => {
                                    this.scoreTypes = res;
                                    this.workflowFieldsService.setAvailableScoreTypes(this.scoreTypes);
                                    let scoreIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'ScoreType');

                                    if (scoreIndex > -1) {
                                        this.model.Event.ScoreType = +this.conditions[scoreIndex]['@Value'];
                                    }
                                    this.validate();
                                });
                        }
                    }))))
            .pipe(concatMap(() => this.workflowService.getWorkflowFieldTypes(this.objectID, this.objectType, true, this.model.Event.IssueObject)
                .pipe(
                    map(r => {
                        r.forEach(t => {
                            let c = this.conditions.filter(c => c['@FieldTypeID'] == t.ID);
                            if (c != null)
                                c.forEach(f => f['@FieldName'] = t.FriendlyName + (t.Object == 'IssueType' ? ' (Action Field)' : ''));
                        });
                    }))))
            .pipe(concatMap(() => of(
                //apply names to contextual fields
                this.conditions.filter(c => c['@ContextualFieldID'] != null).forEach(c => {
                    let cx = this.workflowFieldsService
                        .getContextualFieldsForType()
                        .find(x => x.value == 'Contextual|' + c['@ContextualFieldID']);
                    if (cx != null)
                        c['@FieldName'] = cx.label;
                })
            )))
            .pipe(concatMap(() => of(() => {
                this.loadResponsibilities();
            })))
            .pipe(
                finalize(() => {
                    this.validate();
                    this.model.Event.SettingsObject.Settings.MessageRecipientType = 'SpecificUser';
                    this.isLoading = false;
                })).subscribe();

    }

    loadObjects() {
        return this.workflowService.getWorkflowObjectTypes(this.model.Event.ChangeType)
            .pipe(
                map(r => this.workflowObjectTypes = [this.defaultWorkflowObject].concat(r)),
                map(() => {
                    if (this.hideShoppingCart) {
                        this.workflowObjectTypes = this.workflowObjectTypes.filter(w => w.type != 'ShoppingCartType');
                    }
                })).subscribe();
    }

    changeTypeChanged(event) {
        this.model.Event.ChangeType = event;
        this.showAddCondition = false;

        this.loadContextualFields();

        this.validate();
        this.loadObjects();
    }

    selectObjectType(e: any) {
        this.selectedObjectType = e;
        this.showAddCondition = false;
        this.conditions = [];
        this.scoreTypes = [];

        if (e != null && e.indexOf('|') > -1) {
            this.objectType = e.split('|')[0];
            this.objectID = +e.split('|')[1];

            if (this.model.Event.SettingsObject.Settings.TaxonomyTypeID != null && this.objectType != 'ArtifactType') {
                delete this.model.Event.SettingsObject.Settings.TaxonomyTypeID;
            }

            if (this.model.Event.ChangeType != WorkflowChangeType.Schedule) {
                if (this.model.Event.SettingsObject.Settings.ScheduleInterval != null) {
                    delete this.model.Event.SettingsObject.Settings.ScheduleInterval;
                }
                if (this.model.Event.SettingsObject.Settings.ScheduleDays != null) {
                    delete this.model.Event.SettingsObject.Settings.ScheduleDays;
                }
                if (this.model.Event.SettingsObject.Settings.ScheduleType != null) {
                    delete this.model.Event.SettingsObject.Settings.ScheduleType;
                }
            }

            if (this.objectType == 'IssueType') {
                this.issueObjectTypes = this.workflowObjectTypes.slice().filter(w => w.type != 'IssueType' && w.type != 'ReferenceItemType');
            }

            this.loadContextualFields();
            this.loadResponsibilities();
        } else {
            this.isValid = false;
        }
    }

    selectIssueObjectType(e: any) {
        this.model.Event.IssueObject = e;
        this.loadContextualFields();
    }

    loadResponsibilities() {
        if (this.objectType == null || this.objectID == null) {
            this.responsibilities = [];
        }
        this.resSub = this.responsibilityService.getResponsibilityTypesByObject(this.objectType, this.objectID)
            .subscribe(r => this.responsibilities = r);

        this.validate();
    }

    loadContextualFields() {
        let type = this.objectType;
        let id = this.objectID;

        if (this.model.Event.IssueObject != null && this.model.Event.IssueObject.indexOf('|') > -1) {
            type = this.model.Event.IssueObject.split('|')[0];
            id = +this.model.Event.IssueObject.split('|')[1];
        }

        if (type != null && id != null) {
            if (this.model.Event.ChangeType == WorkflowChangeType.ScoreUpdate
                || this.model.Event.ChangeType == WorkflowChangeType.Update
                || this.model.Event.ChangeType == WorkflowChangeType.RequestCertification
                || this.model.Event.ChangeType == WorkflowChangeType.Schedule) {
                this.workflowService.getScoreTypes(id, type)
                    .subscribe(res => {
                        this.scoreTypes = res;
                        this.scoreTypes.unshift({ label: '', value: null });
                        this.workflowFieldsService.setAvailableScoreTypes(this.scoreTypes);
                    });
            } else {
                this.scoreTypes = [];
                this.workflowFieldsService.setAvailableScoreTypes(this.scoreTypes);
            }
        } else {
            this.scoreTypes = [];
            this.workflowFieldsService.setAvailableScoreTypes(this.scoreTypes);
        }
    }

    runFrequencyMax(): number {
        return this.model.Event.SettingsObject.Settings.ScheduleType == 'h' ? 72 : 365;
    }

    checkScheduleInterval() {
        if (+this.model.Event.SettingsObject.Settings.ScheduleInterval > this.runFrequencyMax())
            this.model.Event.SettingsObject.Settings.ScheduleInterval = this.runFrequencyMax();
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
            .subscribe(x => {
                if (x) {
                    this.warningMessage = $localize`There are pending workflow items for this workflow. These items can still be completed, but no new workflow items will be created.`;
                }
            });
    }

    onStateChange($event) {
        if ($event) {
            this.model.Type.State = State.Active;
        } else {
            this.model.Type.State = State.InActive;
            if (this.model.Type.ID && this.model.Type.ID != 0) this.hasPendingWorkflowItems();
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

        let obj = null;
        let objid = null;

        if (this.model.Event.IssueObject != null && this.model.Event.IssueObject.indexOf('|') > -1) {
            obj = this.model.Event.IssueObject.split('|')[0];
            objid = this.model.Event.IssueObject.split('|')[1];
        }


        //add/remove hidden contextual field conditions for actions and scores
        let contextualFields = [
            { key: 'IssueObject', value: obj, type: 'T', applies: (this.objectType == 'IssueType' && objid != null) },
            { key: 'IssueObjectID', value: objid, type: 'D', applies: (this.objectType == 'IssueType' && objid != null) },
            { key: 'ScoreType', value: this.model.Event.ScoreType, type: 'D', applies: (this.model.Event.ChangeType == WorkflowChangeType.ScoreUpdate) },

        ];

        contextualFields.forEach(field => {
            let ix = this.conditions.findIndex(c => c['@ContextualFieldID'] == field.key);
            if (field.applies) {
                if (ix == -1) {
                    this.conditions.push({
                        '@ContextualFieldID': field.key,
                        '@Operator': '=',
                        '@ValueType': field.type,
                        '@Value': field.value
                    });
                } else {
                    this.conditions[ix]['@Value'] = field.value;
                }
            } else if (ix != -1) {
                this.conditions.splice(ix, 1);
            }
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

        if (this.model.Event.ChangeType != WorkflowChangeType.Schedule) {
            //delete schedule settings
            delete this.model.Event.SettingsObject.Settings.ScheduleType;
            delete this.model.Event.SettingsObject.Settings.ScheduleInterval;
            delete this.model.Event.SettingsObject.Settings.ScheduleDays;
        }

        this.model.Event.Condition = JSON.stringify({ Conditions: { Condition: this.conditions } });
        this.model.Event.Settings = JSON.stringify(this.model.Event.SettingsObject);

        this.onSave.emit(this.model);
    }

    validate() {
        this.errorMessage = "";

        if (this.model == null) return;
        let hasConditionsError = false;
        this.conditions.forEach(c => {
            if (c['@ContextualFieldID'] != null && this.excludedContextualFields.indexOf(c['@ContextualFieldID']) != -1)
                return;

            if (c['@FieldName'] == null && c['@ContextualFieldID'] == null) {
                this.errorMessage = $localize`One or more conditions is not valid.`;
                hasConditionsError = true;
                return;
            }
            if (this.model.Event.ChangeType != WorkflowChangeType.Update && c['@Operator'] == 'C') {
                this.errorMessage = $localize`The value changed operator for conditions may only be used with the Item Changed workflow change type.`;
                hasConditionsError = true;
                return;
            }
        });

        if (hasConditionsError) {
            this.isValid = false;
            return;
        }

        if (this.model.Event.ChangeType == WorkflowChangeType.Schedule && this.selectedObjectType != '' && this.selectedObjectType != null) {
            if (this.scheduleTypes.map(s => s.value).indexOf(this.model.Event.SettingsObject.Settings.ScheduleType) == -1) {
                this.errorMessage = $localize`Please select a Run Interval.`;
                this.isValid = false;
                return;
            }

            if (this.model.Event.SettingsObject.Settings.ScheduleInterval == null) {
                this.errorMessage = $localize`Please enter a Run Frequency.`;
                this.isValid = false;
                return;
            } else if (!Number.isInteger(+this.model.Event.SettingsObject.Settings.ScheduleInterval)) {
                this.errorMessage = $localize`Run Frequency must be an integer.`;
                this.isValid = false;
                return;
            } else if (+this.model.Event.SettingsObject.Settings.ScheduleInterval < 1) {
                this.errorMessage = $localize`Run Frequency must be greater than or equal to 1.`;
                this.isValid = false;
                return;
            } else if (+this.model.Event.SettingsObject.Settings.ScheduleInterval > this.runFrequencyMax()) {
                this.errorMessage = $localize`Run Frequency must be less than or equal to ${this.runFrequencyMax()}`;
                this.isValid = false;
                return;
            }

            if (+this.model.Event.SettingsObject.Settings.ScheduleDays == 0) {
                this.errorMessage = $localize`At least one Run Day must be selected.`;
                this.isValid = false;
                return;
            }

            if (this.conditions.length < 1) {
                this.errorMessage = $localize`At least 1 condition is required when using change type Schedule.`;
                this.isValid = false;
                return;
            }

            let t = this.workflowObjectTypes.find(t => t.value == this.selectedObjectType);

            if (t != null && t.count > this.SCHEDULE_OBJECT_LIMIT) {
                this.errorMessage = $localize`The chosen object type has more than ${this.SCHEDULE_OBJECT_LIMIT} items, which exceeds the limit for change type Schedule.`;
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

        if (this.model.Event.ChangeType == WorkflowChangeType.ScoreUpdate && this.model.Event.ScoreType == null) {
            this.errorMessage = $localize`Please select a score type`;
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

    get objectSelected(): boolean {
        return this.selectedObjectType != null && this.selectedObjectType != '';
    }
}