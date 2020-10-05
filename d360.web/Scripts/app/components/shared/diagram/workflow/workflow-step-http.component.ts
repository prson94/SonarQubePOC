import { Component, Output, EventEmitter, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';
import * as _ from 'lodash';
import { NodeModel } from '../../../../models/workflow.model';

@Component({
    selector: 'd3s-workflow-step-http',
    providers: [WorkflowService],
    templateUrl: 'workflow-step-http.component.html',
    styles: [
        `
        .textarea-editor {
            border: 1px solid #ccc;
            resize: none;
            padding: 8px;
            width: 95%;
        }

        .textarea-editor:focus  {
            outline: none;
        }

    `]
})

export class WorkflowStepHttpComponent extends BaseComponent implements OnChanges, OnInit {
    @Input() step: NodeModel;
    @Output() stepChange: EventEmitter<NodeModel> = new EventEmitter<NodeModel>();
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() formFields = [];
    @Input() httpFields = [];
    @Input() issueObject: string;

    methods = [
        'GET',
        'POST',
        'PUT',
        'DELETE'
    ];

    constructor(
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: SimpleChanges) {
        //let httpFieldsChangedOnly = changes['httpFields'] != null && changes['httpFields'].currentValue != changes['httpFields'].previousValue && changes['httpFields'].currentValue != null && changes.length == 1;
        //if (httpFieldsChangedOnly)

        //this.load();
    }

    load() {
        this.workflowFieldsService.pushHttpFields(this.step);
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

    append(e: string) {
        if (this.step.settings.HTTPRequest.Body == null)
            this.step.settings.HTTPRequest.Body = '';

        this.step.settings.HTTPRequest.Body += e;
        this.stepChange.emit(this.step);
    }
}
