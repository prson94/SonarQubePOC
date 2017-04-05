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
import { Taxonomy } from '../../../models/taxonomy.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';
import { TaxonomiesService } from '../../../services/taxonomies.service';

declare var CompanySettings;

@Component({
    selector: 'd3s-admin-workflow-new-editor',
    providers: [WorkflowService, TaxonomiesService],
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

    WorkflowChangeType = WorkflowChangeType;

    constructor(private workflowService: WorkflowService, private taxonomyService: TaxonomiesService) {
        super();
    }

    ngOnInit() {

        if (CompanySettings.ArtifactType_TaxonomyTypeID != null && CompanySettings.ArtifactType_TaxonomyTypeID != '') {
            this.subjectAreaName = CompanySettings.ArtifactType_TaxonomyTypeID;
        } else {
            this.subjectAreaName = 'Subject Area';
        }

        this.load();

        //create initial model and settings if needed
        if (this.model == null)
            this.model = new WorkflowDiagramModel();
        if (this.model.Event.SettingsObject == null)
            this.model.Event.SettingsObject = {};
        if (this.model.Event.SettingsObject.Settings == null)
            this.model.Event.SettingsObject.Settings = {};

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

                            if (this.model.Event.SettingsObject != null && this.model.Event.SettingsObject.Settings != null) {
                                this.hideObject = (this.model.Event.SettingsObject.Settings.Visible == "false") ? true : false;
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
                            })
                        });
                }
            })
            .then(() => { this.isLoading = false; console.log(this.model) });

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
            //don't store unless needed
            delete this.model.Event.SettingsObject.Settings.TaxonomyTypeID;
        }


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
        console.log(this.conditions);
    }

    remove(item: any) {
        let i = this.conditions.findIndex(c => c == item);
        this.conditions.splice(i, 1);
    }

    save() {
        this.model.Event.SettingsObject.Settings.Visible = !this.hideObject;

        this.model.Event.conditions = this.conditions;
        this.model.Event.Object = this.objectType;
        this.model.Event.ObjectID = this.objectId;

        this.conditions.forEach(c => {
            delete c['@FieldName'];
        });

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
}