import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges } from '@angular/core';
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

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-step-field-change',
    providers: [WorkflowService],
    templateUrl: 'workflow-step-field-change.component.html'
})

export class WorkflowStepFieldChangeComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() fieldUpdate = {};
    @Input() formFields = [];
    @Output() fieldUpdateChange = new EventEmitter();

    private fields: FieldType[] = [];
    private field: FieldType;
    private lookups = [];
    private bools = [
        { value: 'false', label: 'False' },
        { value: 'true', label: 'True' }
    ];
    private hasFormResponses = false;
    private selectedFormFieldId;


    constructor(private workflowService: WorkflowService, private workflowFieldsService: WorkflowFieldsService) {
        super();
    }

    ngOnInit() {
       
    }

    ngOnChanges() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.fields = [];

        this.hasFormResponses = this.formFields != null && this.formFields.length > 0;

        if (!this.hasFormResponses && this.fieldUpdate != null) {
            this.changeUseFormValue(false);
        }

        //console.log(this.formFields);

        this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType)
            .then(r => {
                this.fields = r;
            })
            .then(() => {
                if (this.fieldUpdate != null && this.fieldUpdate['@FieldId'] != null) {
                    this.select(this.fieldUpdate['@FieldId'], false);
                }

                if (this.fieldUpdate['@ClearValue'] != null)
                    this.fieldUpdate['@ClearValue'] = this.fieldUpdate['@ClearValue'].toString().toLowerCase() == 'true' ? true : false;
                if (this.fieldUpdate['@UseCurrentDate'] != null)
                    this.fieldUpdate['@UseCurrentDate'] = this.fieldUpdate['@UseCurrentDate'].toString().toLowerCase() == 'true' ? true : false;
                if (this.fieldUpdate['@UseFormValue'] != null)
                    this.fieldUpdate['@UseFormValue'] = this.fieldUpdate['@UseFormValue'].toString().toLowerCase() == 'true' ? true : false;

                if (this.fieldUpdate['@UseFormValue'] == true) {
                    if (this.fieldUpdate['@FormFieldId'] != null && this.fieldUpdate['@FormStepId'] != null) {
                        this.changeFormValue(this.fieldUpdate['@FormFieldId'] + '|' + this.fieldUpdate['@FormStepId']);
                    } 
                }


            })
            .then(() => this.isLoading = false);
    }

    select(e: any, clear: boolean = true) {
        this.field = null;
        this.fieldUpdate['@FieldId'] = e;

        if (clear)
            delete this.fieldUpdate['@Value'];

        let f = this.fields.find(f => f.ID == +e);
        if (f) this.field = f;

        if (this.field) {
            this.fieldUpdate['@FieldName'] = this.field.FriendlyName;

            if (this.field.Type == 'Lookup') {
                this.workflowService.getLookupList(this.field.ID)
                    .then(r => {
                        this.lookups = r;
                        this.lookups = this.lookups.filter(l => l.value != '');
                    });
            }
        }

        this.fieldUpdateChange.emit(this.fieldUpdate);
    }

    changeDate(e: any) {
        let d = new Date(e);
        let dateString = "";

        dateString = (d.getMonth() + 1).toString()
    }

    changeUseFormValue(e: any) {
        this.fieldUpdate['@UseFormValue'] = e;
        if (!e) {
            delete this.fieldUpdate['@FormFieldId'];
            delete this.fieldUpdate['@FormStepId'];
        }
        this.fieldUpdateChange.emit(this.fieldUpdate);
    }

    changeFormValue(e: any) {
        this.selectedFormFieldId = e;
        let field = this.formFields.find(f => f['@FormFieldId'] == e);
        if (field == null) {
            this.fieldUpdate['@FormFieldId'] = null;
            return;
        }
        this.fieldUpdate['@FormFieldId'] = field['@id'];
        this.fieldUpdate['@FormStepId'] = field['@stepId'];

        this.fieldUpdateChange.emit(this.fieldUpdate);
    }
}

class FieldUpdate {
    FieldId: string;
    Value: string;
    UseCurrentDate: boolean = false;
}