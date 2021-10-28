import { Component, Output, EventEmitter, Input, OnInit } from "@angular/core";
import { BaseComponent } from "../../../shared/base.component";
import * as _ from "lodash";
import { HTTPResponseOutput, NodeModel } from "../../../../models/workflow.model";
import { WorkflowFieldsService } from "../../../../services/workflow-fields.service";
import { CompanySettingsService } from "../../../../services/settings.service";

@Component({
    selector: "d3s-workflow-step-http-response",
    templateUrl: "workflow-step-http-response.component.html",
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

export class WorkflowStepHttpResponseComponent extends BaseComponent implements OnInit {
    @Input() step: NodeModel;
    @Input() diagram: go.Diagram;
    @Output() stepChange: EventEmitter<NodeModel> = new EventEmitter<NodeModel>();
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() formFields = [];
    @Input() httpFields = [];
    @Input() issueObject: string;


    isAdding = false;
    isDeleting = false;
    isEditing = false;
    selectedIndex = 0;
    selectedRow = null;

    menuItems: any[] = [
        { title: "Edit" },
        { title: "Delete" },
    ];

    httpRequests = [];

    constructor(
        protected settingsService: CompanySettingsService,
        private workflowFieldsService: WorkflowFieldsService) {
        super(settingsService);
    }

    ngOnInit() {
        this.filterHttpRequestFields();

        this.workflowFieldsService.httpRequest$.subscribe(() => {
            this.filterHttpRequestFields();
        });
    }

    clickMenu(e: any) {
        switch (e.value.toLowerCase()) {
            case "edit":
                this.showEdit();
                break;
            case "delete":
                this.showDelete();
                break;
        }
    }

    removeOutput() {
        this.workflowFieldsService.deleteOutputField(this.selectedRow.StepId, this.selectedRow.Id);
        this.step.settings.HTTPResponse.Outputs.splice(this.selectedIndex, 1);
        
        this.cancel();
        this.stepChange.emit(this.step);
    }

    showAdd() {
        this.cancel();
        this.isAdding = true;
    }

    showEdit() {
        this.cancel();

        let selected = this.step.settings.HTTPResponse.Outputs[this.selectedIndex];

        this.selectedRow = new HTTPResponseOutput();
        this.selectedRow.Name = selected.Name;
        this.selectedRow.Path = selected.Path;
        this.selectedRow.StepId = selected.StepId;
        this.selectedRow.Id = selected.Id;
        this.isEditing = true;
    }

    showDelete() {
        this.cancel();
        this.selectedRow = this.step.settings.HTTPResponse.Outputs[this.selectedIndex];
        this.isDeleting = true;
    }

    select(i: number) {
        this.cancel();
        this.selectedIndex = i;
        this.selectedRow = this.step.settings.HTTPResponse.Outputs[i];
    }

    cancel() {
        this.isAdding = false;
        this.isDeleting = false;
        this.isEditing = false;
        this.selectedRow = new HTTPResponseOutput();

        let len = this.step.settings.HTTPResponse.Outputs.length;
        let count = len === 0 ? 1 : this.step.settings.HTTPResponse.Outputs
            .map((f) => +(f.Id))
            .sort((a, b) => { return a - b; })[len - 1] + 1;

        this.selectedRow.StepId = this.step.key;
        this.selectedRow.Id = count;
    }

    addOutput() {
        this.step.settings.HTTPResponse.Outputs.push(this.selectedRow);
        this.workflowFieldsService.pushOutputField(this.selectedRow);
        this.stepChange.emit(this.step);
        this.cancel();
    }

    editOutput() {
        this.step.settings.HTTPResponse.Outputs[this.selectedIndex] = this.selectedRow;
        this.workflowFieldsService.updateOutputField(this.selectedRow);
        this.cancel();
    }

    filterHttpRequestFields() {
        this.httpRequests = [];
        if (this.step == null || this.diagram == null) {
            return;
        }

        let fields = this.workflowFieldsService.getHttpRequestFields();
        let upstreamSteps = [];
        this.traverseDiagram(this.step.key, upstreamSteps);

        fields.forEach((f) => {
            let k = upstreamSteps.filter((u) => u === f.key);
            if (k != null && k.length > 0) {
                this.httpRequests.push({
                    key: f.key,
                    name: f.name
                });
            }
        });
    }

    traverseDiagram(key: any, upstreamSteps: any[]) {
        let steps = <any[]>this.diagram.model.nodeDataArray;
        let links = <any[]>(<go.GraphLinksModel>this.diagram.model).linkDataArray;

        let step = steps.find((s) => s.key === key);
        let toLinks = links.filter((l) => l.to === key);

        if (_.includes(upstreamSteps, key)) {
            return;
        }
        upstreamSteps.push(step.key);

        if (toLinks == null || toLinks.length < 1) {
            return;
        }

        toLinks.forEach((l) => this.traverseDiagram(l.from, upstreamSteps));
    }
}
