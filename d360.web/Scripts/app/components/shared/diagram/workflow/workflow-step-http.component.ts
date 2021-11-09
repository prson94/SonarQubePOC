import { Component, Output, EventEmitter, Input, OnInit } from "@angular/core";
import { BaseComponent } from "../../../shared/base.component";
import * as _ from "lodash";
import { NodeModel } from "../../../../models/workflow.model";
import { WorkflowFieldsService } from "../../../../services/workflow-fields.service";
import { CompanySettingsService } from "../../../../services/settings.service";

@Component({
    selector: "d3s-workflow-step-http",
    templateUrl: "workflow-step-http.component.html",
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

export class WorkflowStepHttpComponent extends BaseComponent implements OnInit  {
    @Input() step: NodeModel;
    @Input() diagram: go.Diagram;
    @Output() stepChange: EventEmitter<NodeModel> = new EventEmitter<NodeModel>();
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() formFields = [];
    @Input() httpFields = [];
    @Input() outputFields = [];
    @Input() issueObject: string;

    methods = [
        "GET",
        "POST",
        "PUT",
        "DELETE"
    ];

    constructor(
        protected settingsService: CompanySettingsService,
        private workflowFieldsService: WorkflowFieldsService) {
        super(settingsService);
    }

    ngOnInit() {
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
