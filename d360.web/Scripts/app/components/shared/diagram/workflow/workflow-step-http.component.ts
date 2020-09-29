import { Component, Output, EventEmitter, Input, OnChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';
import * as _ from 'lodash';
import { NodeModel } from '../../../../models/workflow.model';

@Component({
    selector: 'd3s-workflow-step-http',
    providers: [WorkflowService],
    templateUrl: 'workflow-step-http.component.html'
})

export class WorkflowStepHttpComponent extends BaseComponent implements OnChanges {
    @Input() step: NodeModel;
    @Output() stepChange: EventEmitter<NodeModel> = new EventEmitter<NodeModel>();
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() formFields = [];
    @Input() issueObject: string;

    methods = [
        'GET',
        'POST',
        'PUT',
        'DELETE'
    ];

    constructor(private workflowService: WorkflowService, private workflowFieldsService: WorkflowFieldsService) {
        super();
    }

    ngOnChanges() {
        this.load();
    }

    load() {
        //this.isLoading = true;
    }

    initField(f: any) {

    }  

    get asJson(): string {
        //TODO: remove before final check in
        return JSON.stringify(this.step);
    }

    removeHeader(i: number) {
        this.step.settings.HTTPRequest.Headers.splice(i, 1);
        this.stepChange.emit(this.step);
    }

    addHeader() {
        this.step.settings.HTTPRequest.Headers.push({ key: null, value: null });
        this.stepChange.emit(this.step);

    }
}
