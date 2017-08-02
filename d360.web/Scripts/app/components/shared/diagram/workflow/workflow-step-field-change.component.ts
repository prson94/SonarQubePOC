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
    @Output() fieldUpdateChange = new EventEmitter();

    private fields: FieldType[] = [];
    private field: FieldType;
    private lookups = [];
    private bools = [
        { value: 'false', label: 'False' },
        { value: 'true', label: 'True' }
    ];


    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
       
    }

    ngOnChanges() {
        console.log(this.fieldUpdate);
        this.load();
    }

    load() {
        this.fields = [];
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
            });
    }

    select(e: any, clear: boolean = true) {
        this.field = null;
        this.fieldUpdate['@FieldId'] = e;

        if (clear)
            delete this.fieldUpdate['@Value'];

        let f = this.fields.find(f => f.ID == +e);
        if (f) this.field = f;

        if (this.field) {
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
}

class FieldUpdate {
    FieldId: string;
    Value: string;
    UseCurrentDate: boolean = false;
}