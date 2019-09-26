import { Component, NgZone, OnInit, Output, EventEmitter, Input, OnChanges } from '@angular/core';
import { Router } from '@angular/router';
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
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
    selector: 'd3s-workflow-history',
    providers: [WorkflowService],
    templateUrl: './workflow-history.component.html'
})

export class WorkflowHistoryComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() versionStepId: number;
    @Input() versionStepTransitionFromId: number;
    @Input() versionStepTransitionToId: number;
    @Input() filteredObject: string;
    @Input() filteredObjectId: number;

    selection: any;

    history: any[];
    FormMode = FormMode;
    WorkflowActivityType = WorkflowActivityType;
    formMode = FormMode.Default;


    constructor( private workflowService: WorkflowService, private router: Router) {
        super();
        
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {
        this.load();
    }

    load() {
        this.history = [];
        if (this.versionStepId != null) {
            this.isLoading = true;
            this.workflowService.getWorkflowVersionStepHistory(this.versionStepId, this.filteredObject, this.filteredObjectId)
                .subscribe(r => {
                    this.history = r;
                    this.isLoading = false;
                });
        }
    }

    export() {
        this.workflowService.exportVersionStepHistory(this.versionStepId, this.filteredObject, this.filteredObjectId);
    }

    navigate(url: string) {
        this.router.navigateByUrl(url);
    }
}


enum FormMode {
    Default,
    ShowFormHistory,
}