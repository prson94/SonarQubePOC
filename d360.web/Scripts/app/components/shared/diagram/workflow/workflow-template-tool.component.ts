import { Component, OnInit, Output, EventEmitter, Input, AfterViewChecked, ViewChild, SimpleChanges, OnDestroy } from "@angular/core";
import * as _ from "lodash";
import { WorkflowService } from "../../../../services/workflow.service";
import { WorkflowFieldsService } from "../../../../services/workflow-fields.service";
import { HTTPResponseOutput, NodeModel } from "../../../../models/workflow.model";

@Component({
    selector: "d3s-workflow-template-tool",
    providers: [WorkflowService],
    templateUrl: "./workflow-template-tool.component.html"
})

export class WorkflowTemplateToolComponent implements OnInit, AfterViewChecked, OnDestroy {
    @Input() objectType: string;
    @Input() objectId: number;
    @Output() onItemClick = new EventEmitter();
    @Input() issueObject: string;
    @Input() step: NodeModel;
    @Input() diagram: any;
    @ViewChild("sel", { static: false }) select;
    @ViewChild("cont", { static: false }) container;

    fields = [];
    httpFields = [];
    httpFieldsSub: any;
    outputFields: HTTPResponseOutput[] = [];
    outputFieldsSub: any;

    defaultFields = [
        { value: "[OBJECT_NAME]", label: "Object Name" },
        { value: "[OBJECT_TYPE]", label: "Object Type" },
        { value: "[ASSET_PATH]", label: "Object Asset Path" },
        { value: "[GOV_SCORE]", label: "Object Governance Score" },
        { value: "[DQ_SCORE]", label: "Object Data Quality Score" },
        { value: "[GOV_SCORE_PREV]", label: "Previous Governance Score" },
        { value: "[DQ_SCORE_PREV]", label: "Previous Data Quality Score" },
        { value: "[WORKFLOW_INITIATOR]", label: "Workflow Initiator Name" },
        { value: "[WORKFLOW_INITIATOR_UID]", label: "Workflow Initiator UID" },
        { value: "[ACTION_DETAILS]", label: "Action Details" },
        { value: "[RECIPIENT_TYPE]", label: "Recipient Type" },
        { value: "[RECIPIENT_RESPONSIBILITY]", label: "Recipient Responsibility" },
        { value: "[WORKFLOW_STEP_ID]", label: "Workflow Step ID" },
        { value: "[WORKFLOW_ID]", label: "Workflow ID" },
        { value: "[WORKFLOW_INSTANCE_ID]", label: "Workflow Instance ID" },
        { value: "[ASSET_UID]", label: "Asset UID" },
    ];

    relationshipFields = [
        { value: "[REL_SUBJECT_NAME]", label: "Relationship Subject Name" },
        { value: "[REL_OBJECT_NAME]", label: "Relationship Object Name" },
        { value: "[REL_SUBJECT_UID]", label: "Relationship Subject UID" },
        { value: "[REL_OBJECT_UID]", label: "Relationship Object UID" },
    ];

    selected = "none";

    constructor(private workflowService: WorkflowService, private workflowFieldsService: WorkflowFieldsService) {
    }

    ngOnInit() {
        this.fields = _.cloneDeep(this.defaultFields);

        this.httpFieldsSub = this.workflowFieldsService.httpFields$.subscribe(() => {
            this.filterHttpFields();
            this.load();
        });

        this.outputFieldsSub = this.workflowFieldsService.outputFields$.subscribe(() => {
            this.filterOutputFields();
            this.load();
        });

    }

    ngOnChanges(changes: SimpleChanges) {
        let objectTypeChanged = changes["objectType"] != null && changes["objectType"].currentValue !== changes["objectType"].previousValue && changes["objectType"].currentValue != null;
        let objectIdChanged = changes["objectId"] != null && changes["objectId"].currentValue !== changes["objectId"].previousValue && changes["objectId"].currentValue != null;
        let stepChanged = changes["step"] != null && changes["step"].currentValue !== changes["step"].previousValue && changes["step"].currentValue != null;

        if (objectTypeChanged || objectIdChanged || stepChanged) {
            this.filterHttpFields();
            this.filterOutputFields();
            this.load();
        }

    }

    ngOnDestroy() {
        if (this.httpFieldsSub) {
            this.httpFieldsSub.unsubscribe();
        }
        if (this.outputFieldsSub) {
            this.outputFieldsSub.unsubscribe();
        }
    }

    load() {
        this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType, true, this.issueObject)
            .subscribe((r) => {
                this.fields = [];
                this.fields = _.cloneDeep(this.defaultFields);

                if (this.objectType === "IntersectType") {
                    this.fields = this.fields.concat(_.cloneDeep(this.relationshipFields));
                }

                r.forEach((f) => {
                    let fieldType = f.Object === "IssueType" ? "Action Field" : "Asset Field";

                    this.fields.push({
                        value: (f.Type === "JsonElement" ? "[JSON" : "[FIELD") + f.ID + "]#[" + fieldType + " :: " + f.Name + "]",
                        label: fieldType + " :: " + f.Name
                    });
                });

                this.httpFields.forEach((f) => {
                    let label = "HTTP Request :: " + f["@label"];
                    this.fields.push({
                        value: "[HTTPREQUEST|" + f["@stepId"] + "|" + f["@id"] + "]",
                        label
                    });
                });

                this.outputFields.forEach((f) => {
                    let label = "HTTP Response :: " + f.Name;
                    this.fields.push({
                        value: "[HTTPRESPONSE|" + f.StepId + "|" + f.Id+ "]",
                        label
                    });
                });
            });
    }

    ngAfterViewChecked() {
        //Workaround for quill until primeng supports better quill API access

        //quill generates 'display: none' on <select> nodes
        //update it here
        this.select.nativeElement.style.display = "inline-block";

        //remove auto-generated spans
        for (let i = 0; i < this.container.nativeElement.childNodes.length; i++) {
            let node = this.container.nativeElement.childNodes[i];

            if (node.tagName === "SPAN") {
                this.container.nativeElement.removeChild(node);
            }
        }
    }

    filterHttpFields() {
        this.httpFields = [];
        if (this.step == null || this.diagram == null) {
            this.load();
            return;
        }

        let fields = this.workflowFieldsService.getHttpFields();
        let upstreamSteps = [];
        this.traverseDiagram(this.step.key, upstreamSteps);

        fields.forEach((f) => {
            let k = upstreamSteps.filter((u) => u === f["@stepId"]);
            if (k != null && k.length > 0) {
                f["@FormFieldId"] = f["@id"] + "|" + f["@stepId"];
                f["@FormLabel"] = "HTTP Request :: " + f["@label"];
                this.httpFields.push(f);
            }
        });
    }

    filterOutputFields() {
        this.outputFields = [];
        if (this.step == null || this.diagram == null) {
            this.load();
            return;
        }

        let fields = this.workflowFieldsService.getOutputFields();
        let upstreamSteps = [];
        this.traverseDiagram(this.step.key, upstreamSteps);

        fields.forEach((f) => {
            let k = upstreamSteps.filter((u) => u === f.StepId);
            if (k != null && k.length > 0) {
                f["@FormFieldId"] = f.Id + "|" + f.StepId;
                f["@FormLabel"] = "HTTP Response :: " + f.Name;
                this.outputFields.push(f);
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

    clickItem(e: any) {
        if (e === "none") {
            return;
        }

        let f = e.split('#');
        if (f.length === 2) {
            this.onItemClick.emit(f[1]);
        }
        else {
            this.onItemClick.emit(f[0]);
        }
        this.selected = "none";
        this.select.nativeElement.value = "none";
    }

}
