import { Component, Output, EventEmitter, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import * as _ from 'lodash';
import { NodeModel } from '../../../../models/workflow.model';

@Component({
    selector: 'd3s-workflow-step-http',
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

export class WorkflowStepHttpComponent extends BaseComponent {
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

    constructor() {
        super();
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
