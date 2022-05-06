import { Component, OnInit, Output, EventEmitter, Input, OnChanges, ViewChild } from "@angular/core";
import * as _ from "lodash";
import { Editor } from "primeng/editor";

import { BaseComponent } from "../../../shared/base.component";
import {
    NodeModel,
    WorkflowForm,
    WorkflowFormFieldType,
    FormResponseType,
    EmailTaskRecipientType,
    NodeSettings,
    NodeFields,
    WorkflowChangeType,
} from "../../../../models/workflow.model";
import { WorkflowService } from "../../../../services/workflow.service";
import { WorkflowFieldsService } from "../../../../services/workflow-fields.service";
import { GroupService } from "../../../../services/group.service";
import { FormMode, SelectItem } from "../../../../models/form.model";
import { forkJoin } from "rxjs";
import { CompanySettingsService } from "../../../../services/settings.service";

@Component({
    selector: "d3s-workflow-step-form-editor",
    providers: [WorkflowService, GroupService],
    templateUrl: "./workflow-step-form-editor.component.html"
})

export class WorkflowStepFormEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() step: NodeModel;
    @Input() diagram: go.Diagram;
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() issueObject: string;
    @Input() httpFields: any[] = [];
    @Input() outputFields: any[] = []
    @Input() workflowChangeType: WorkflowChangeType;
    @Output() stepChange = new EventEmitter();
    @ViewChild("ed", { static: false }) ed: Editor;
    @ViewChild("fed", { static: false }) fed: Editor;
    private quill;

    private model: WorkflowForm = new WorkflowForm();

    private originalStep: NodeModel;
    WorkflowFormFieldType = WorkflowFormFieldType;
    FormMode = FormMode;
    private newField: any = {};
    private formMode = FormMode.Default;
    private usedIn: any[] = [];
    private deletingField;
    private selectedIndex: number = 0;
    private selectedRow: any = null;

    private usedFields: any[] = [];
    private showHelp = false;

    private intersectType = null;

    private destination = [];
    private groups: SelectItem[] = [];
    private lookups = null;
    private intersectTypes = null;
    private isListLoading = false;

    private allowReassignResource = false;
    private allowReassignObject = false;

    menuEditLabel = $localize`Edit`;
    menuDeleteLabel = $localize`Delete`;
    menuMoveToTopLabel = $localize`Move to Top`;
    menuMoveUpTopLabel = $localize`Move Up`;
    menuMoveDownLabel = $localize`Move Down`;
    menuMoveToBottomLabel = $localize`Move to Bottom`;

    private baseMenuItems: any[] = [
        { title: this.menuEditLabel },
        { title: this.menuDeleteLabel },
    ];

    private upMenuItems: any[] = [
        { title: this.menuMoveToTopLabel },
        { title: this.menuMoveUpTopLabel }
    ];

    private downMenuItems: any[] = [
        { title: this.menuMoveDownLabel },
        { title: this.menuMoveToBottomLabel }
    ];

    private types = [
        { value: WorkflowFormFieldType.Boolean, label: "boolean" },
        { value: WorkflowFormFieldType.Integer, label: "integer" },
        { value: WorkflowFormFieldType.Text, label: "text" },
        { value: WorkflowFormFieldType.Date, label: "date" },
        { value: WorkflowFormFieldType.List, label: "list" },
        { value: WorkflowFormFieldType.RelationshipType, label: "relationshipType" },
        { value: WorkflowFormFieldType.HTML, label: "html" },
        { value: WorkflowFormFieldType.Link, label: "link" },

    ];

    FormResponseType = FormResponseType;
    EmailTaskRecipientType = EmailTaskRecipientType;

    private responseTypes = [
        { value: FormResponseType[FormResponseType.FirstResponse], label: "First Response" },
        { value: FormResponseType[FormResponseType.Majority], label: "Majority" },
        { value: FormResponseType[FormResponseType.All], label: "All" },
    ];

    constructor(
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService,
        private groupService: GroupService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnInit() {
        this.originalStep = _.cloneDeep(this.step);
        let promises = [];

        this.usedFields = this.workflowFieldsService.getUsedFields();


        if (this.destination.length < 1) {
            this.isLoading = true;

            forkJoin(
                this.workflowService.getEmailTaskRecipientType(),
                this.groupService.getGroups()
            ).subscribe(
                (
                    [
                        EmailTaskRecipientList,
                        GroupList
                    ]
                ) => {
                    EmailTaskRecipientList.forEach((e) => {

                        if (e.ID < 1 || e.ID === EmailTaskRecipientType.Followers) {
                            return;
                        }
                        else if (e.ID === EmailTaskRecipientType.Initiator) {
                            if (this.workflowChangeType === WorkflowChangeType.ScoreUpdate || this.workflowChangeType === WorkflowChangeType.Schedule) {
                                return;
                            }
                        }

                        this.destination.push({
                            value: EmailTaskRecipientType[e.ID],
                            label: e.Name
                        });
                    });
                    /* ./EmailTaskRecipientList */

                    /* GroupList */
                    this.groups = GroupList.items.map(g => { return { value: g.Uid, label: g.Name } });
                    if (this.step.settings.MessageToGroup != null) {
                        if (!this.groups.find((g) => g.value === this.step.settings.MessageToGroup)) {
                            this.groups.push(<SelectItem>{ value: this.step.settings.MessageToGroup, label: "<invalid group>" });
                        }
                    }
                    /* ./GroupList */

                    this.isLoading = false;
                }
            );
        }
    }

    ngOnChanges() {
        this.initFields();

        if (this.ed != null && this.ed.quill != null) {
            this.quill = this.ed.quill;
        } else {
            this.quill = null;
        }
    }

    ngAfterViewChecked() {
        if (this.ed != null && this.ed.quill != null) {
            this.quill = this.ed.quill;
        }
    }

    ngOnDestroy() {
        this.quill = null;
        this.ed = null;
    }

    initFields() {
        //deal with xml-json nonsense
        if (this.step.fields == null || this.step.fields.form == null) {
            this.step.fields = new NodeFields();
            this.step.fields.form.field = [];
        }
        if (this.step.fields.form.field == null) {
            this.step.fields.form.field = [];
        }

        if (this.step.fields.form.field.length == null) {
            let f = _.cloneDeep(this.step.fields.form.field);
            this.step.fields.form.field = [];
            this.step.fields.form.field.push(f);
        }

        if (this.step.settings == null) {
            this.step.settings = new NodeSettings();
        }

        //parse bool fields
        if (this.step.settings.SendFormEmail == null) {
            this.step.settings.SendFormEmail = false;
        }
        else {
            this.step.settings.SendFormEmail = this.step.settings.SendFormEmail.toString().toLowerCase() === "true" ? true : false;
        }

        if (this.step.settings.IncludePreviousFormResponses == null) {
            this.step.settings.IncludePreviousFormResponses = false;
        }
        else {
            this.step.settings.IncludePreviousFormResponses = this.step.settings.IncludePreviousFormResponses.toString().toLowerCase() === "true" ? true : false;
        }

        if (this.step.fields.form["@allowReassignObject"] != null) {
            this.allowReassignObject = this.step.fields.form["@allowReassignObject"].toString().toLowerCase() === "true" ? true : false;
        }

        if (this.step.fields.form["@allowReassignResource"] != null) {
            this.allowReassignResource = this.step.fields.form["@allowReassignResource"].toString().toLowerCase() === "true" ? true : false;
        }

        this.usedFields = this.workflowFieldsService.getUsedFields();

        //load lists, needed for labels
        this.changeType("list");
        this.changeType("relationshipType");
    }

    add() {
        this.formMode = FormMode.Adding;
    }

    remove() {
        let item = this.step.fields.form.field[this.selectedIndex];
        this.deletingField = item;

        this.usedIn = [];
        this.usedIn = this.usedFields.filter((u) => u.stepId === this.step.key && u.fieldId === item["@id"]);

        this.formMode = FormMode.Deleting;
    }

    edit() {
        let item = this.step.fields.form.field[this.selectedIndex];
        this.usedIn = [];
        this.usedIn = this.usedFields.filter((u) => u.stepId === this.step.key && u.fieldId === item["@id"]);

        let i = this.step.fields.form.field.find((f) => f["@id"] === item["@id"]);
        this.newField = i;
        if ((item["@required"] === "true" || item["@required"] === true || (item["@type"] === "boolean"))) {
            this.newField["@required"] = true;
        }
        else {
            this.newField["@required"] = false;
        }
        this.newField["@oldId"] = this.newField["@id"];
        this.newField["@oldType"] = this.newField["@type"];

        //trigger load of type list
        this.changeType(this.newField["@type"]);

        this.formMode = FormMode.Editing;
    }

    move(offset) {
        let item = this.step.fields.form.field[this.selectedIndex];
        let nextItem = this.step.fields.form.field[this.selectedIndex + offset];

        this.step.fields.form.field[this.selectedIndex] = nextItem;
        this.step.fields.form.field[this.selectedIndex + offset] = item;

        this.selectedIndex += offset;
        this.select(this.selectedIndex);
    }

    moveTop() {
        let first = this.step.fields.form.field.splice(0, 1)[0];
        let item = this.step.fields.form.field.splice(this.selectedIndex - 1, 1)[0];

        this.step.fields.form.field.unshift(first);
        this.step.fields.form.field.unshift(item);

        this.select(0);
    }

    moveBottom() {
        let lastIndex = this.step.fields.form.field.length - 1;
        let item = this.step.fields.form.field.splice(this.selectedIndex, 1)[0];

        this.step.fields.form.field.push(item);
        this.select(lastIndex);
    }

    confirmDelete() {
        let i = this.step.fields.form.field.findIndex((f) => f["@id"] === this.deletingField["@id"]);

        if (i >= 0) {
            this.step.fields.form.field.splice(i, 1);

            //primeng v4.1 issue
            let fields = _.cloneDeep(this.step.fields.form.field);
            this.step.fields.form.field = null;
            this.step.fields.form.field = fields;

            //another prime issue. prime adds _$visited property sometimes, fix pending release
            //but we need to remove it to avoid polluting the XML
            this.step.fields.form.field.forEach((f) => {
                if (f["_$visited"]) {
                    delete f["_$visited"];
                }
            });

            this.stepChange.emit(this.step);
            this.deletingField["@stepId"] = this.step.key;
            this.workflowFieldsService.deleteFormField(this.deletingField);

        }

        this.formMode = FormMode.Default;
    }

    cancel() {
        this.formMode = FormMode.Default;
        this.newField = {};
    }

    save() {
        //calculate the next id # based on existing fields
        let newFieldType = this.newField["@type"]?.toLowerCase();
        let len = this.step.fields.form.field.filter((f) => f["@type"]?.toLowerCase() === newFieldType).length;
        let count = len === 0 ? 1 : this.step.fields.form.field
            .filter((f) => f["@type"]?.toLowerCase() === newFieldType)
            .map((f) => +(f["@id"]?.toLowerCase().replace(newFieldType, "")))
            .sort((a, b) => { return a - b; })[len - 1] + 1;

        let typeChanged = (this.newField["@oldType"] !== this.newField["@type"]);
        let existing = null;
        let f = {};

        if (this.newField["@oldId"] != null) {
            if (typeChanged) {
                let i = this.step.fields.form.field.findIndex((f) => f["@id"] === this.newField["@oldId"]);

                if (i >= 0) {
                    existing = _.cloneDeep(this.step.fields.form.field[i]);
                    this.step.fields.form.field.splice(i, 1);
                }

                this.newField["@id"] = this.newField["@type"].toString().toLowerCase() + count.toString();
            } else {
                existing = this.step.fields.form.field.find((e) => e["@id"] === this.newField["@id"]);
            }

            delete this.newField["@oldType"];
            delete this.newField["@oldId"];
        }
        else
            this.newField["@id"] = this.newField["@type"].toString().toLowerCase() + count.toString();

        if (existing != null) {
            this.workflowFieldsService.deleteFormField({"@stepId":this.step.key,"@id":existing["@id"]});
            f = existing;
        }

        f["@id"] = this.newField["@id"];
        f["@label"] = this.newField["@label"];
        f["@type"] = this.newField["@type"];
        f["@required"] = this.newField["@required"];
        if (this.newField["@type"] === "list") {
            f["@referenceFieldId"] = this.newField["@referenceFieldId"];
        }
        else {
            delete f["@referenceFieldId"];
        }

        f["@stepId"] = this.step.key;

        if (existing == null || typeChanged) {
            this.step.fields.form.field.push(_.cloneDeep(this.newField));
        }
        else {
            this.workflowFieldsService.forceFormFieldUpdate();
        }

        this.newField = {};
        this.formMode = FormMode.Default;
        this.stepChange.emit(this.step);

        this.step.fields.form.field = this.step.fields.form.field.slice();
        this.workflowFieldsService.pushFormField(f);
    }

    appendFieldDescription(e: string) {
        if (this.fed != null && this.fed.quill != null) {
            this.quill = this.fed.quill;
        }

        if (this.quill != null) {
            let pos = this.quill.getSelection(true);
            let len = pos.index || this.quill.getLength();
            this.quill.insertText(len > 0 ? len - 1 : 0, e, "api");

            //manually set the html in the model
            this.step.fields.form["@description"] = this.quill.container.querySelector(".ql-editor").innerHTML;

        } else {
            this.step.fields.form["@description"] =
                ((this.step.fields.form["@description"] == null) ? "" :
                this.step.fields.form["@description"])
                + e;
        }
        this.stepChange.emit(this.step);
    }

    appendField(e: string) {
        if (this.ed != null && this.ed.quill != null) {
            this.quill = this.ed.quill;
        }

        if (this.quill != null) {
            let pos = this.quill.getSelection(true);
            let len = pos.index || this.quill.getLength();
            this.quill.insertText(len > 0 ? len - 1 : 0, e, "api");

            //manually set the html in the model
            this.step.settings.MessageBodyTemplate = this.quill.container.querySelector(".ql-editor").innerHTML;

        } else {
            this.step.settings.MessageBodyTemplate =
                ((this.step.settings.MessageBodyTemplate == null) ? "" :
                    this.step.settings.MessageBodyTemplate)
                + e;
        }

        this.stepChange.emit(this.step);
    }

    changeType(e: any) {
        this.newField["@type"] = e;

        let type = this.objectType;
        let id = this.objectId;
        let hasIssueObject = false;

        if (this.issueObject.indexOf("|") > -1) {
            hasIssueObject = true;
            type = this.issueObject.split("|")[0];
            id = +this.issueObject.split("|")[1];
        }

        if (e === "boolean") {
            this.newField["@required"] = true;
        }
        if (e === "relationshipType" && this.intersectTypes == null) {
            this.workflowService.getAllowIntersectTypes(type, id)
                .subscribe((r) => {
                    this.intersectTypes = r;
                });
        }
        if (e === "list" && this.lookups == null) {
            this.workflowService.getWorkflowVersionStepFormLookups(this.objectType, this.objectId, (hasIssueObject ? type : null), (hasIssueObject ? id : null))
                .subscribe((r) => {
                    this.lookups = r;
                });
        }
    }

    private mapHTMLToFormProperty(html: string, prop: string) {
        if (html == null) {
            delete this.step.fields.form[prop];
        }
        else {
            this.step.fields.form[prop] = html;
        }
    }

    private getTypeLabel(i: any) {
        switch (i["@type"]) {
            case "list":
                if (this.lookups == null) {
                    return "List";
                }
                let list = this.lookups.find((l) => l.value.toString() === i["@referenceFieldId"]);
                return "List" + (list == null ? "" : " :: " + list.label);
            case "relationshipType":
                if (this.intersectTypes == null) {
                    return "Relationship";
                }
                let rel = this.intersectTypes.find((l) => l.IntersectTypeID.toString() === i["@intersectTypeId"]);
                return "Relationship" + (rel == null ? "" : ( " :: " + ((rel.PredicateName != null && rel.PredicateName.length > 0) ? `[${rel.PredicateName}] ` : " ") + rel.TargetName));
            default:
                return (i["@type"].charAt(0).toUpperCase() + i["@type"].substr(1));
        }
    }

    validateField() {
        if (this.newField["@label"] == null || this.newField["@label"].length < 1 || this.newField["@type"] == null || this.newField["@type"] === "") {
            return false;
        }

        if (this.newField["@type"] === "list" && this.newField["@referenceFieldId"] == null) {
            return false;
        }

        if (this.newField["@type"] === "relationshipType" && (this.newField["@intersectTypeId"] == null || this.newField["@intersectTypeId"] === "")) {
            return false;
        }

        return true;
    }

    menuItems(includeUp: boolean, includeDown: boolean): any[] {
        if (includeUp && includeDown) {
            return this.baseMenuItems
                .concat(this.upMenuItems)
                .concat(this.downMenuItems);
        } else if (includeUp) {
            return this.baseMenuItems.concat(this.upMenuItems);
        } else if (includeDown) {
            return this.baseMenuItems.concat(this.downMenuItems);
        } else {
            return this.baseMenuItems;
        }
    }

    clickMenu(e: any) {
        switch (e.value.toLowerCase()) {
            case this.menuEditLabel.toLowerCase():
                this.edit();
                break;
            case this.menuDeleteLabel.toLowerCase():
                this.remove();
                break;
            case this.menuMoveUpTopLabel.toLowerCase():
                this.move(-1);
                break;
            case this.menuMoveToTopLabel.toLowerCase():
                this.moveTop();
                break;
            case this.menuMoveDownLabel.toLowerCase():
                this.move(1);
                break;
            case this.menuMoveToBottomLabel.toLowerCase():
                this.moveBottom();
                break;
        }
    }

    select(index: number) {
        this.selectedIndex = index;
        this.selectedRow = this.step.fields.form.field[index];
    }
}
