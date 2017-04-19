import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
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
} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-step-editor',
    providers: [WorkflowService, ResponsibilityTypeService],
    templateUrl: './workflow-step-editor.component.html'
})

export class WorkflowStepEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() step: NodeModel;
    @Output() stepChange = new EventEmitter();

    WorkflowActivityType = WorkflowActivityType;

    private originalStep: NodeModel;
    private status = [
        'Draft',
        'Under Review',
        'Certified'
    ];

    private destination = [
        { value: 'Initiator', label: 'Initiator' },
        { value: 'Owner', label: 'Owner' },
        { value: 'SpecificUser', label: 'Specific User' },
    ];

    private responsibilities = [];
    private procedures: WorkflowTaskProcedure[] = [];

    constructor(private responsibilityService: ResponsibilityTypeService, private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
    }

    ngOnChanges() {
        if (this.step.settings == null)
            this.step.settings = {};
        this.originalStep = _.cloneDeep(this.step);

        if (this.step.activityType == WorkflowActivityType.EmailNotification) {
            this.responsibilityService.getResponsibilityTypes()
                .then(r => {
                    this.responsibilities = r;
                    console.log(r);
                });
        } else if (this.step.activityType == WorkflowActivityType.Procedure) {
            this.workflowService.getWorkflowProcedures()
                .then(r => {
                    this.procedures = r;
                });
        }
    }

}