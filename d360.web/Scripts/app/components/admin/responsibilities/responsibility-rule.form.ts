import { Input, Output, Component, EventEmitter, OnInit } from "@angular/core";
import { SelectItem } from "primeng/api";
import { ResponsibilityTypeService } from "../../../services/responsibility-type.service";
import {    
    ResponsibilityTypeRelationRule,
    ResponsibilityTypeRelationRuleDefinition,
    ResponsibilityTypeRelationRuleDefinitionWhenItem,
    ResponsibilityTypeRelationRuleDefinitionWhenTestRow,
    ResponsibilityTypeRelationRuleDefinitionThen,
    ResponsibilityTypeRelationRuleDefinitionThenItem,
    ResponsibilityTypeRelationRuleDefinitionThenTestRow,
    ResponsibilityTypeRelationRuleFormDataFieldType
} from "../../../models/responsibility-type.model";
import { ObjectDetailService } from "../../../services/object-detail.service";
import { BaseComponent } from "../../shared/base.component";
import * as _ from "lodash";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { CompanySettingsService } from "../../../services/settings.service";


@Component({
    selector: "d3s-responsibility-rule-form",
    templateUrl: "./responsibility-rule.form.html",
    styles: [
        `
        .display-table tr td {
            padding:3px;
            border-radius: 0;
        }
        .relation-table tr td {
            border-radius: 0;
        }
        
       .display-table-title {
            text-align:center;
            width:100%;
            text-transform: uppercase;
            color: #5c5e60 !important;
            font-size: 1rem;
            font-weight: bold;
        }`
    ],
    providers: [ResponsibilityTypeService, ObjectDetailService],
})

export class ResponsibilityRuleForm extends BaseComponent implements OnInit {
    @Input() ruleId: number;
    @Input() id: number;

    @Output() onComplete = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    public isThenTestLoading: boolean = false;
    public isWhenTestLoading: boolean = false;

    model: ResponsibilityTypeRelationRule = new ResponsibilityTypeRelationRule();
    disableTestWhen: boolean = false;
    disableTestThen: boolean = false;

    actionName: string = "Add";

    private objectTypes: SelectItem[] = [];
    whenCheckTypes: SelectItem<string>[] = [
        { label: "Field", value: "F" },
        { label: "Relationship", value: "R" }
    ];
    private whenBoolTypes: SelectItem[] = [
        { label: "Choose...", value: null },
        { label: "True", value: "true" },
        { label: "False", value: "false" },
    ];
    private whenFieldTypes: ResponsibilityTypeRelationRuleFormDataFieldType[] = [];
    private whenIntersectTypes: SelectItem<number>[] = [];
    WhenTestRows: ResponsibilityTypeRelationRuleDefinitionWhenTestRow[] = [];
    ThenTestRows: ResponsibilityTypeRelationRuleDefinitionThenTestRow[] = [];

    thenObjectTypes: SelectItem<string>[] = [
        { label: "Choose...", value: null },
        { label: "Group", value: "GroupType" },
        { label: "Organization", value: "OrganizationType" },
        { label: "User", value: "ResourceType" }
    ];
    private thenFieldTypes: ResponsibilityTypeRelationRuleFormDataFieldType[] = [];

    errorMessage: string = "";

    constructor(
        private responsibilityTypeService: ResponsibilityTypeService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private objectDetailService: ObjectDetailService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }
        
    private load(): void {
        if (this.id > 0) {
            this.actionName = "Edit";
            this.isLoading = true;
            this.responsibilityTypeService.getRelationOptionsByResponsibilityType(this.ruleId)
                .subscribe((d) => {
                    this.objectTypes = d;
                    this.objectTypes.unshift({ label: "Choose...", value: null });
                });
            let r: ResponsibilityTypeRelationRule;
            this.responsibilityTypeService.getResponsibilityTypeRelationRule(this.id)
                .subscribe((data) => {
                    this.model = data;
                    this.model.ObjectString = this.model.Object + "|" + this.model.ObjectID;
                    r = data;
                    this.responsibilityTypeService.getRelationRuleFormData(this.model.StructuredDefinition.Then.Object, this.model.StructuredDefinition.Then.ObjectID)
                        .subscribe((d) => {
                            this.thenFieldTypes = d.FieldTypes;
                            this.thenFieldTypes.unshift({ label: "Choose...", value: null, type: null, isLookup: false, values: [] });
                            this.responsibilityTypeService.getRelationRuleFormData(this.model.Object, this.model.ObjectID)
                                .subscribe((d) => {
                                    this.whenFieldTypes = d.FieldTypes;
                                    this.whenIntersectTypes = d.IntersectTypes;

                                    if (this.model.StructuredDefinition.When) {
                                        this.model.StructuredDefinition.When.forEach((wft) => this.loadWhenValuesForFieldType(wft));
                                    }

                                    this.whenFieldTypes.unshift({ label: "Choose...", value: null, type: null, isLookup: false, values: [] });
                                    this.whenIntersectTypes.unshift({ label: "Choose...", value: null });
                                });
                            this.model = r;
                            this.model.ObjectString = r.Object + "|" + r.ObjectID;
                            this.isLoading = false;
                            //load the then islookup and field values
                            if (this.model && this.model.StructuredDefinition && this.model.StructuredDefinition.Then && this.model.StructuredDefinition.Then.Conditions != null && this.model.StructuredDefinition.Then.Conditions.length > 0) {
                                for (let item of this.model.StructuredDefinition.Then.Conditions) {
                                    this.loadThenValuesForFieldType(item, false);
                                }
                            }

                            if (!this.model.StructuredDefinition.Then.Conditions) {
                                this.model.StructuredDefinition.Then.Conditions = [];
                            }
                        })
                })
        } else {
            this.actionName = "Add";
            this.isLoading = true;

            // Instantiate the object and its properties.
            this.model = new ResponsibilityTypeRelationRule();
            this.model.IsVisible = true;
            this.model.ApplyToType = false;
            this.model.ResponsibilityTypeID = this.ruleId;
            this.model.StructuredDefinition = new ResponsibilityTypeRelationRuleDefinition();
            this.model.StructuredDefinition.When = [];
            this.model.StructuredDefinition.Then = new ResponsibilityTypeRelationRuleDefinitionThen();
            this.model.StructuredDefinition.Then.Conditions = [];

            this.responsibilityTypeService.getRelationOptionsByResponsibilityType(this.ruleId)
                .subscribe((d) => {
                    this.objectTypes = d;
                    this.objectTypes.unshift({ label: "Choose...", value: null });
                    this.isLoading = false;
                });
        }
    }

    loadObjectType(value: string): Promise<void> {
        let promises = [];
        if (value == null) {
            return Promise.resolve();
        }

        var otData = value.split("|");

        this.model.Object = otData[0];
        this.model.ObjectID = +otData[1];

        this.model.StructuredDefinition.When = [];
        this.model.StructuredDefinition.Then.Object = "";
        this.model.StructuredDefinition.Then.Conditions = [];
        this.responsibilityTypeService.getRelationRuleFormData(this.model.Object, this.model.ObjectID)
            .subscribe((d) => {
                this.whenFieldTypes = d.FieldTypes;
                this.whenIntersectTypes = d.IntersectTypes;
                let excluded = this.whenFieldTypes.findIndex((a) => a.label === "Choose...");
                if (excluded < 0) {
                    this.whenFieldTypes.unshift({ label: "Choose...", value: null, type: null, isLookup: false, values: [] });
                }
                excluded = this.whenIntersectTypes.findIndex((a) => a.label === "Choose...");
                if (excluded < 0) {
                    this.whenIntersectTypes.unshift({ label: "Choose...", value: null });
                }
            });

        return Promise.all(promises).then(() => { });
    }

    // Clear When Filter array when "Applies To Entire Type" selected
    clearWhen(): void {
        if (this.model.StructuredDefinition.When) {
            this.model.StructuredDefinition.When.splice(0, this.model.StructuredDefinition.When.length);
        }
    }


    addWhen(): void {
        let whenItem: ResponsibilityTypeRelationRuleDefinitionWhenItem = new ResponsibilityTypeRelationRuleDefinitionWhenItem();
        whenItem.CheckType = "F";
        whenItem.IsBool = false;
        if (!this.model.StructuredDefinition.When) {
            this.model.StructuredDefinition.When = [];
        }
        this.model.StructuredDefinition.When.push(whenItem);
    }

    private loadWhenValuesForFieldType(item: ResponsibilityTypeRelationRuleDefinitionWhenItem): Promise<void> {
        item.IsBool = false;
        if (item.FieldTypeID) {
            let selectedFieldType = this.whenFieldTypes.find((f) => f.value === item.FieldTypeID);
            if (selectedFieldType) {
                selectedFieldType = _.cloneDeep(selectedFieldType);
                item.FieldTypeName = selectedFieldType.label;
                if (selectedFieldType.isLookup) {
                    let excluded = selectedFieldType.values.findIndex(a => a.label == "Choose...");
                    if (excluded < 0) {
                        selectedFieldType.values.unshift({ label: "Choose...", value: null });
                    }

                    item.ValueOptions = selectedFieldType.values;
                    item.IsLookup = selectedFieldType.isLookup;
                }
                else if (selectedFieldType.type === "Boolean") {
                    item.IsBool = true;
                    item.ValueOptions = this.whenBoolTypes;
                    item.IsLookup = selectedFieldType.isLookup;
                }
                else {
                    item.ValueOptions = [];
                    item.IsLookup = selectedFieldType.isLookup;
                }
            }
            else {
                item.ValueOptions = [];
                item.IsLookup = false;
            }
        }
        else {
            let selectedIntersectType = this.whenIntersectTypes.find((f) => f.value === item.IntersectTypeID);
            if (selectedIntersectType) {
                this.loadValuesForIntersectType(item);
            }
            else {
                item.ValueOptions = [];
                item.IsLookup = false;
            }
        }

        return null;
    }

    parseRelationshipWhenValue(item: ResponsibilityTypeRelationRuleDefinitionWhenItem): Promise<void> {
        if (item.Value) {
            item.TargetObject = item.Value.split("|")[0];
            item.TargetObjectID = parseInt(item.Value.split("|")[1]);
        }
        return null;
    }

    removeWhenCondition(i: number): void {
        this.model.StructuredDefinition.When.splice(i, 1);
    }

    testWhen(): Promise<void> {

        this.isWhenTestLoading = true;

        let promises = [];
        this.disableTestWhen = true;

        const whenTest = _.cloneDeep(this.model);

        //remove valueoptions from any when criteria
        if (whenTest.StructuredDefinition.When) {
            whenTest.StructuredDefinition.When.forEach((wft) => {
                wft.ValueOptions = [];
            });
        }

        this.responsibilityTypeService.testWhen(whenTest)
            .subscribe((d) => {
                this.WhenTestRows = d;
                this.disableTestWhen = false;
                this.isWhenTestLoading = false;
            });

        return Promise.all(promises).then(() => { });
    }

    addThenCondition(): void {
        let thenItem: ResponsibilityTypeRelationRuleDefinitionThenItem = new ResponsibilityTypeRelationRuleDefinitionThenItem();
        this.model.StructuredDefinition.Then.Conditions.push(thenItem);
    }

    loadThenFilterOptions(value: string): Promise<void> {
        let promises = [];

        if (value == null) {
            return Promise.resolve();
        }

        this.model.StructuredDefinition.Then.Object = value;
        this.model.StructuredDefinition.Then.ObjectID = 1;

        this.responsibilityTypeService.getRelationRuleFormData(this.model.StructuredDefinition.Then.Object, this.model.StructuredDefinition.Then.ObjectID)
            .subscribe((d) => {
                this.thenFieldTypes = d.FieldTypes;
                this.thenFieldTypes.unshift({ label: "Choose...", value: null, type: null, isLookup: false, values: [] });
            });

        return Promise.all(promises).then(() => { });
    }

    removeThenCondition(i: number): void {
        this.model.StructuredDefinition.Then.Conditions.splice(i, 1);
    }

    testThen(): Promise<void> {

        this.isThenTestLoading = true;

        let promises = [];
        this.disableTestThen = true;

        const thenTest = _.cloneDeep(this.model);

        //remove valueoptions from any when criteria
        if (thenTest.StructuredDefinition.When) {
            thenTest.StructuredDefinition.When.forEach(wft => {
                wft.ValueOptions = [];
            });
        }

        this.responsibilityTypeService.testThen(thenTest)
            .subscribe((d) => {
                this.ThenTestRows = d;
                this.disableTestThen = false;
                this.isThenTestLoading = false;
            });

        return Promise.all(promises).then(() => { });
    }

    private loadThenValuesForFieldType(item: any, clearValue?: boolean): Promise<void> {
        let selectedFieldType = this.thenFieldTypes.find((f) => f.value === item.FieldTypeID);
        if (clearValue !== undefined && clearValue === true) item.Value = "";
        if (selectedFieldType) {
            item.IsBool = false;
            item.FieldTypeName = selectedFieldType.label;
            if (selectedFieldType.isLookup) {
                let excluded = selectedFieldType.values.findIndex(a => a.label == "Choose...");
                if (excluded < 0) {
                    selectedFieldType.values.unshift({ label: "Choose...", value: null });
                }
                item.ValueOptions = selectedFieldType.values;
                item.IsLookup = selectedFieldType.isLookup;
            }
            else if (selectedFieldType.type == "Boolean") {
                item.IsBool = true;
                item.ValueOptions = this.whenBoolTypes;
                item.IsLookup = selectedFieldType.isLookup;
            }
            else {
                item.ValueOptions = [];
                item.IsLookup = selectedFieldType.isLookup;
            }
        }
        else {
            item.ValueOptions = [];
            item.IsLookup = false;
        }
        return null;
    }

    private loadValuesForIntersectType(item: ResponsibilityTypeRelationRuleDefinitionWhenItem): Promise<void> {
        item.IsloadValuesForIntersectType = true;
        this.responsibilityTypeService.getRelationRuleFormDataRelationshipsForDropdown(this.model.Object, this.model.ObjectID, item.IntersectTypeID)
            .subscribe((d) => {
                item.IsloadValuesForIntersectType = false;
                item.IsBool = false;
                item.ValueOptions = d;
                let excluded = item.ValueOptions.findIndex((a) => a.label === "Choose...");
                if (excluded < 0) {
                    item.ValueOptions.unshift({ label: "Choose...", value: null });
                }
            });
        return null;
    }

    private isValid(): boolean {
        if (!this.model.ApplyToType) {
            if (!this.model.StructuredDefinition.When || this.model.StructuredDefinition.When.length === 0) {
                return false;
            }
            else {
                return true;
            }
        }
        else {
            return true;
        }
    }
    cancel(): void {
        this.onCancel.emit(null);
    }

    onSubmit(): any {
        this.isLoading = true;

        //remove valueoptions from any when criteria
        if (this.model.StructuredDefinition.When) {
            this.model.StructuredDefinition.When.forEach(wft => {
                wft.ValueOptions = [];
            });
        }

        if (this.model.ID > 0) {
            this.responsibilityTypeService.putRule(this.model)
                .subscribe((r) => {
                    this.isLoading = false;
                    this.showMessageForResult(this.messagesService, r);
                    if (r.type != "error") {
                        this.onComplete.emit({ action: "edit", field: this.model });
                    }
                });
        } else {
            this.responsibilityTypeService.postRule(this.model)
                .subscribe((r) => {
                    this.showMessageForResult(this.messagesService, r);
                    this.isLoading = false;
                    if (r.type != "error") {
                        this.onComplete.emit({ action: "add", field: this.model });
                    }
                });
        }
    }
}
