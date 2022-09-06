import { Input, Output, Component, EventEmitter, OnInit } from "@angular/core";
import { SelectItem } from "primeng/api";
import { ResponsibilityTypeService } from "../../../services/responsibility-type.service";
import {
    ResponsibilityTypeRelationRule,
    ResponsibilityTypeRelationRuleDefinition,
    ResponsibilityTypeRelationRuleDefinitionWhenItem,
    ResponsibilityTypeRelationRuleDefinitionThen,
    ResponsibilityTypeRelationRuleDefinitionThenItem,
    ResponsibilityTypeRelationRuleFormDataFieldType,
    RuleThenV2,
    ResponsibilityRuleTestRow
} from "../../../models/responsibility-type.model";
import { ObjectDetailService } from "../../../services/object-detail.service";
import { BaseComponent } from "../../shared/base.component";
import * as _ from "lodash";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { CompanySettingsService } from "../../../services/settings.service";
import { HelpResource } from "../../../models/resource.model";
import { Operator } from "../../../models/operator.model";
import { forEach } from "core-js/fn/dict";
import { forkJoin } from "rxjs";


@Component({
    selector: "d3s-responsibility-rule-form",
	templateUrl: "./responsibility-rule.form.html",
	styleUrls: ["responsibility-rule.less"],
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

	readonly resourceType: string = "ResourceType";
	readonly groupType: string = "GroupType";

    model: ResponsibilityTypeRelationRule = new ResponsibilityTypeRelationRule();
    disableTestWhen: boolean = false;
    disableTestThen: boolean = false;

	showWhenResults: boolean = true;
	showThenResults: boolean = true;
	noWhenResults: boolean = false;
	noThenResults: boolean = false;

    actionName: string = $localize`Add`;

    visibleTooltip = $localize`Marking the rule as visible applies a visibility setting to all applied responsibilities for this rule. Users will be able to see users and groups assigned via this rule. 
                    Setting the rule to not be visible simply hides the users that are assigned via the rule. Permissions are still applied.`;

    applyToTooltip = $localize`Marking the rule as applying to a type grants the applied responsibilities to act upon the type itself, including all instances under the type. 
                    For example, if you wanted to grant a user or group with the ability to create a specific business term you would need to check this box.`;

    addCheckTitle = $localize`Add Condition`;
    testButtonLabel = $localize`Test Filters`;
    addConditionLabel = $localize`Add condition`;

    matchAllLabel = $localize`Match all`;
    matchAnyLabel = $localize`Match any`;

    saveLabel = $localize`Add Rule`;
	cancelLabel = $localize`Cancel`;

	showResultsLabel = $localize`Show Results`;
	hideResultsLabel = $localize`Hide Results`;

    chooseLabel = $localize`Choose...`;

    private objectTypes: SelectItem[] = [];
    whenCheckTypes: SelectItem<string>[] = [
        { label: $localize`Field`, value: "F" },
        { label: $localize`Relationship`, value: "R" }
    ];
    private whenBoolTypes: SelectItem[] = [
        { label: $localize`True`, value: "true" },
        { label: $localize`False`, value: "false" },
	];

	private fieldOperatorTypes: SelectItem[] = [
		{ label: "contains", value: Operator[Operator.Contains] },
		{ label: "does not contain", value: Operator[Operator.NotContains] },
		{ label: "is", value: Operator[Operator.Equals] },
		{ label: "is not", value: Operator[Operator.NotEquals] },
		{ label: "starts with", value: Operator[Operator.StartsWith] },
		{ label: "ends with", value: Operator[Operator.EndsWith] },
		{ label: "is populated", value: Operator[Operator.Populated] },
		{ label: "is not populated", value: Operator[Operator.NotPopulated] }]

	private relationshipOperatorTypes: SelectItem[] = [
		{ label: "in", value: Operator[Operator.In] },
		{ label: "not in", value: Operator[Operator.NotIn] }]

    private whenFieldTypes: ResponsibilityTypeRelationRuleFormDataFieldType[] = [];
    private whenIntersectTypes: (SelectItem<number> & { uid: string })[] = [];
    WhenTestRows: ResponsibilityRuleTestRow[] = [];
    ThenTestRows: ResponsibilityRuleTestRow[] = [];

    thenObjectTypes: SelectItem<string>[] = [
        { label: $localize`Group`, value: this.groupType },
        { label: $localize`User`, value: this.resourceType }
    ];
	private thenUserFieldTypes: ResponsibilityTypeRelationRuleFormDataFieldType[] = [];
	private thenGroupFieldTypes: ResponsibilityTypeRelationRuleFormDataFieldType[] = [];

	errorMessage: string = "";

	public simpleWhenFilter: string = "";
	public simpleThenFilter: string = "";

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
            this.actionName = $localize`Edit`;
            this.isLoading = true;
            this.responsibilityTypeService.getRelationOptionsByResponsibilityType(this.ruleId)
                .subscribe((d) => {
                    this.objectTypes = this.mapObjectTypes(d);
                });
            let r: ResponsibilityTypeRelationRule;
            this.responsibilityTypeService.getResponsibilityTypeRelationRule(this.id)
                .subscribe((data) => {
					this.model = data;
					if (this.model.StructuredDefinition.Then && this.model.StructuredDefinition.Then.Conditions) {
						this.model.StructuredDefinition.Then.Conditions.forEach((x) => {
							if (!x.Object) {
								x.Object = this.model.StructuredDefinition.Then.Object;
								x.ObjectID = this.model.StructuredDefinition.Then.ObjectID;
							}							
						});
					}
                    this.model.ObjectString = this.model.Object + "|" + this.model.ObjectID + "|" + this.model.AssetTypeUid.toLowerCase();
                    r = data;

                            this.model = r;
                            this.model.ObjectString = r.Object + "|" + r.ObjectID + "|" + r.AssetTypeUid.toLowerCase();
                            //load the then islookup and field values
                            

                            if (!this.model.StructuredDefinition.Then.Conditions) {
								this.model.StructuredDefinition.Then.Conditions = [];
								this.addThenCondition();
							}

							this.responsibilityTypeService.getRelationRuleFormData(this.model.Object, this.model.ObjectID)
								.subscribe((w) => {
									this.whenFieldTypes = w.FieldTypes;
									this.whenIntersectTypes = w.IntersectTypes;

									if (this.model.StructuredDefinition.When) {
										this.model.StructuredDefinition.When.forEach((wft) => this.loadWhenValuesForFieldType(wft));
									}

									let resources = this.responsibilityTypeService.getRelationRuleFormData(this.resourceType, 1);
									let groups = this.responsibilityTypeService.getRelationRuleFormData(this.groupType, 1);

									if (this.model && this.model.StructuredDefinition && this.model.StructuredDefinition.Then) {
										forkJoin([
											resources,
											groups
										]).subscribe(([resourceList, groupList]) => {
											this.thenUserFieldTypes = resourceList.FieldTypes;
											this.thenGroupFieldTypes = groupList.FieldTypes;

											this.model.StructuredDefinition.Then.Conditions.forEach((t) => {
												if (t.Value && t.FieldTypeID && !t.Object && this.model.StructuredDefinition.Then.Object && this.model.StructuredDefinition.Then.Object.length > 0) {
													t.Object = this.model.StructuredDefinition.Then.Object;
												}
												this.loadThenValuesForFieldType(t, false);
											});
											this.isLoading = false;
										});
									}																																
								});
                });
        } else {
            this.actionName = $localize`Add`;
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
                    this.objectTypes = this.mapObjectTypes(d);
                    this.isLoading = false;
				});
        }
    }

    mapObjectTypes(input: SelectItem<string>[]) {
        let mapped = input.map((item) => {
            const [object, objectID, assetTypeUid] = item.value.split("|");
            const mappedValue = `${object}|${objectID}|${assetTypeUid.toLowerCase()}`;
            return ({
                ...item,
                value: mappedValue
            });
        });
        return mapped;
    }

    loadObjectType(value: string): Promise<void> {
        let promises = [];
        if (value == null) {
            return Promise.resolve();
        }

        var otData = value.split("|");

        this.model.Object = otData[0];
        this.model.ObjectID = +otData[1];
        this.model.AssetTypeUid = otData[2];

        this.model.StructuredDefinition.When = [];
        this.model.StructuredDefinition.Then.Object = "";
        this.model.StructuredDefinition.Then.Conditions = [];
        this.responsibilityTypeService.getRelationRuleFormData(this.model.Object, this.model.ObjectID)
            .subscribe((d) => {
                this.whenFieldTypes = d.FieldTypes;
                this.whenIntersectTypes = d.IntersectTypes;
            });
		this.addWhen();
		this.addThenCondition();
        return Promise.all(promises).then(() => { });
    }

    // Clear When Filter array when "Applies To Entire Type" selected
	clearWhen(): void {
		if (this.model.ApplyToType && this.model.StructuredDefinition.When) {
			this.model.StructuredDefinition.When.splice(0, this.model.StructuredDefinition.When.length);
		} else {
			this.addWhen();
		}
    }


    addWhen(): void {
        let whenItem: ResponsibilityTypeRelationRuleDefinitionWhenItem = new ResponsibilityTypeRelationRuleDefinitionWhenItem();
        whenItem.IsBool = false;
        if (!this.model.StructuredDefinition.When) {
            this.model.StructuredDefinition.When = [];
        }
        this.model.StructuredDefinition.When.push(whenItem);
    }

    private loadWhenValuesForFieldType(item: ResponsibilityTypeRelationRuleDefinitionWhenItem, event: any = null): Promise<void> {
		item.IsBool = false;
		item.IsLookup = false;
		item.IsSimpleText = false;
        if (item.FieldTypeID) {
            let selectedFieldType = this.whenFieldTypes.find((f) => f.value === item.FieldTypeID);
            if (selectedFieldType) {
                selectedFieldType = _.cloneDeep(selectedFieldType);
                item.FieldTypeName = selectedFieldType.fieldTypeName ?? selectedFieldType.label;
                if (selectedFieldType.isLookup) {

                    item.ValueOptions = selectedFieldType.values;
                    item.IsLookup = selectedFieldType.isLookup;
                }
                else if (selectedFieldType.type === "Boolean") {
                    item.IsBool = true;
                    item.ValueOptions = this.whenBoolTypes;
                    item.IsLookup = selectedFieldType.isLookup;
				}
				else if (selectedFieldType.type === "Text") {
					item.IsSimpleText = true;		
					if (!item.Operator) {
						if (item.Value) {
							item.Operator = Operator[Operator.Equals];
						} else {
							item.Operator = Operator[Operator.Contains];
						}						
					}
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

        if (event) {
            item.Value = null;
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
		if (this.model.StructuredDefinition.When.length === 0) {
			this.addWhen();
		}
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

        this.responsibilityTypeService
            .testWhenV2({
                AssetTypeUid: this.model.AssetTypeUid,
                Definition: {
                    When: whenTest.StructuredDefinition.When.map((when) => this.mapToWhenV2(when)),
                    Then: []
				}
			},
				this.simpleWhenFilter)
            .subscribe((response) => {
                if (response) {
                    this.WhenTestRows = response.items;
				}
				this.noWhenResults = this.WhenTestRows.length === 0 && this.simpleWhenFilter.trim() === "";

                this.disableTestWhen = false;
                this.isWhenTestLoading = false;
            });

        return Promise.all(promises).then(() => { });
    }

    mapToWhenV2(when: ResponsibilityTypeRelationRuleDefinitionWhenItem) {
        switch (when.CheckType) {
            case "F":
                return ({
                    Field: {
                        ApiName: when.FieldTypeName,
						Value: this.mapToWhenFieldValueForV2(when),
						Operator: when.Operator
                    }
                });

            case "R":
                return ({
                    Relation: {
                        AssetUid: this.mapToWhenRelationAssetUidForV2(when),
						IntersectTypeUid: this.mapToWhenRelationIntersectTypeUidForV2(when),
						Operator: when.Operator
                    }
                });
        }
    }

    mapToWhenFieldValueForV2(when: ResponsibilityTypeRelationRuleDefinitionWhenItem) {
        if (!when.IsLookup) {
            return when.Value;
        }

        const field = this.whenFieldTypes.find((field) => field.value === when.FieldTypeID);
		const item = field.values.find((item) => item.value === when.Value);
		return item.value;
    }

    mapToWhenRelationIntersectTypeUidForV2(when: ResponsibilityTypeRelationRuleDefinitionWhenItem) {
        const intersectType = this.whenIntersectTypes.find((i) => i.value === when.IntersectTypeID);
        return intersectType.uid;
    }

    mapToWhenRelationAssetUidForV2(when: ResponsibilityTypeRelationRuleDefinitionWhenItem) {
        const item = when.IntersectTypeValueOptions.find((i) => i.value === when.Value);
        return item.assetUid;
    }

    addThenCondition(): void {
        let thenItem: ResponsibilityTypeRelationRuleDefinitionThenItem = new ResponsibilityTypeRelationRuleDefinitionThenItem();
        this.model.StructuredDefinition.Then.Conditions.push(thenItem);
    }

    loadThenFilterOptions(item: any): Promise<void> {
        let promises = [];

		if (item == null) {
            return Promise.resolve();
        }

        this.model.StructuredDefinition.Then.Object = item.Object;
		this.model.StructuredDefinition.Then.ObjectID = item.ObjectID = 1;
		if ((item.Object === this.resourceType && (!this.thenUserFieldTypes || this.thenUserFieldTypes.length === 0)) || (item.Object === this.groupType && this.thenGroupFieldTypes.length === 0)) {
			this.responsibilityTypeService.getRelationRuleFormData(item.Object, 1)
				.subscribe((d) => {
					if (item.Object === this.resourceType) {
						this.thenUserFieldTypes = d.FieldTypes;
					} else {
						this.thenGroupFieldTypes = d.FieldTypes;
					}

					this.loadThenValuesForFieldType(item, false);
				});
		} else {
			this.loadThenValuesForFieldType(item, false);
		}
		

        return Promise.all(promises).then(() => { });
    }

    removeThenCondition(i: number): void {
		this.model.StructuredDefinition.Then.Conditions.splice(i, 1);
		if (this.model.StructuredDefinition.Then.Conditions.length === 0) {
			this.addThenCondition();
		}
    }

    testThen(): Promise<void> {

        this.isThenTestLoading = true;

        let promises = [];
        this.disableTestThen = true;

        const thenTest = _.cloneDeep(this.model);

        //remove valueoptions from any when criteria
        if (thenTest.StructuredDefinition.When) {
            thenTest.StructuredDefinition.When.forEach((wft) => {
                wft.ValueOptions = [];
            });
        }

        this.responsibilityTypeService
            .testThenV2({
                AssetTypeUid: this.model.AssetTypeUid,
                Definition: {
                    When: [],
                    Then: [
                        {
                            AssigneeTypeUid: this.getAssigneeTypeUid(thenTest.StructuredDefinition.Then.Conditions[0]),
                            MatchType: thenTest.StructuredDefinition.Then.MatchType as ('and' | 'or'),
                            Conditions: thenTest.StructuredDefinition.Then.Conditions.map((then) => this.mapToThenV2(then))
                        }
                    ]
                }
			},
				this.simpleThenFilter )
            .subscribe((response) => {
                if (response) {
                    this.ThenTestRows = response.items;
				}
				this.noThenResults = this.ThenTestRows.length === 0 && this.simpleThenFilter.trim() === "";
                this.disableTestThen = false;
                this.isThenTestLoading = false;
            });

        return Promise.all(promises).then(() => { });
    }

    getAssigneeTypeUid(then: ResponsibilityTypeRelationRuleDefinitionThenItem) {
		var fieldTypes = then.Object === this.resourceType ? this.thenUserFieldTypes : this.thenGroupFieldTypes;

		if (then == null) {
			return fieldTypes
                .map((field) => field.assigneeTypeUid)
                .filter((x) => x != null)[0];
        }
        
		const field = fieldTypes.find((field) => field.value === then.FieldTypeID);
        return field.assigneeTypeUid;
    }

	mapToThenV2(then: ResponsibilityTypeRelationRuleDefinitionThenItem): RuleThenV2 {
		var fieldTypes = then.Object === this.resourceType ? this.thenUserFieldTypes : this.thenGroupFieldTypes;
        const field = fieldTypes.find((field) => field.value === then.FieldTypeID);
        if (field.isLookup) {
			const item = field.values.find((item) => item.value === then.Value);

            if (item.assigneeUid) {
                return {
					AssigneeTypeUid: field.assigneeTypeUid,
                    Assignee: {
                        Uid: item.assigneeUid
                    }
                };
            }
            else {
				return {
					AssigneeTypeUid: field.assigneeTypeUid,
                    Field: {
                        ApiName: then.FieldTypeName,
						Value: item.value,
						Operator: then.Operator
                    }
                };
            }
        }

		return {
			AssigneeTypeUid: field.assigneeTypeUid,
            Field: {
                ApiName: then.FieldTypeName,
				Value: then.Value,
				Operator: then.Operator
            }
        };
    }

	private loadThenValuesForFieldType(item: any, clearValue?: boolean): Promise<void> {
		var fieldTypes = item.Object === this.resourceType ? this.thenUserFieldTypes : this.thenGroupFieldTypes;
		let selectedFieldType = fieldTypes.find((f) => f.value === item.FieldTypeID);
        if (clearValue !== undefined && clearValue === true) item.Value = "";
        if (selectedFieldType) {
			item.IsBool = false;
			item.IsSimpleText = false;
			item.IsLookup = false;
			item.FieldTypeName = selectedFieldType.fieldTypeName ?? selectedFieldType.label;
            if (selectedFieldType.isLookup) {
                item.ValueOptions = selectedFieldType.values;
                item.IsLookup = selectedFieldType.isLookup;
            }
            else if (selectedFieldType.type == "Boolean") {
                item.IsBool = true;
				item.ValueOptions = this.whenBoolTypes;
				item.IsLookup = selectedFieldType.isLookup;
			} else if (selectedFieldType.type === "Text") {
				item.IsSimpleText = true;
				
				if (!item.Operator) {
					if (item.Value) {
						item.Operator = Operator[Operator.Equals];
					} else {
						item.Operator = Operator[Operator.Contains];
					}
				}
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
                item.IntersectTypeValueOptions = d;
				if (!item.Operator) {
					item.Operator = Operator[Operator.In];
				}

            });
        return null;
    }

	private isValid(): boolean {
		if (!this.model.ApplyToType) {
			if (!this.model.StructuredDefinition.When || this.model.StructuredDefinition.When.length === 0 || !this.isWhenValid()) {
				return false;
			}
		}

		return this.isThenValid();		
	}

	isThenValid() {
		if (this.model.StructuredDefinition.Then.Conditions.length === 0 || !this.model.StructuredDefinition.Then.Conditions[this.model.StructuredDefinition.Then.Conditions.length-1].Object) {
			return true;
		}
		
		if (this.model.StructuredDefinition.Then.Conditions.every((c) => c.FieldTypeID >= 0
			&& c.FieldTypeName && c.FieldTypeName.length > 0
			&& c.Object && c.Object.length > 0
			&& (
				(
					!c.IsSimpleText && (c.Value && c.Value.length > 0)
				)
				||
				(
					c.IsSimpleText && c.Operator && c.Operator.length > 0
					&& (
						(c.Operator === Operator[Operator.Populated] || c.Operator === Operator[Operator.NotPopulated]) || (c.Value && c.Value.length > 0)
					)
				)
			)
		)) {

			return true;
		}
		return false;
	}

	isWhenValid() {
		if (this.model.StructuredDefinition.When.every((w) =>
			w.CheckType && w.CheckType.length > 0
			&& (
				(
					w.CheckType === 'F' && w.FieldTypeID >= 0
					&& w.FieldTypeName && w.FieldTypeName.length > 0
					&& (
						(
							!w.IsSimpleText && (w.Value && w.Value.length > 0)
						)
						||
						(
							w.IsSimpleText && w.Operator && w.Operator.length > 0
							&& (
								(w.Operator === Operator[Operator.Populated] || w.Operator === Operator[Operator.NotPopulated]) || (w.Value && w.Value.length > 0)
							)
						)
					)
				)
				||
				(
					w.CheckType === 'R' && w.IntersectTypeID && w.IntersectTypeID > 0 && w.Operator && w.Value && w.Value.length > 0
				)
			)
		)
		) {
			return true;
		}
		return false;
	}

    cancel(): void {
        this.onCancel.emit(null);
    }

    onSubmit(): any {
        this.isLoading = true;

        //remove valueoptions from any when criteria
        if (this.model.StructuredDefinition.When) {
            this.model.StructuredDefinition.When.forEach((wft) => {
                wft.ValueOptions = [];
            });
		}

		this.model.StructuredDefinition.Then.Conditions = this.model.StructuredDefinition.Then.Conditions.filter((c) => c.FieldTypeID && c.Object && (c.Value || c.Operator === Operator[Operator.Populated] || c.Operator === Operator[Operator.Populated] ));
		if (this.model.StructuredDefinition.Then.Conditions.length === 0) {
			this.model.StructuredDefinition.Then === null;
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
	

	showValueOption(item) {
		if (item.CheckType === "F" && item.FieldTypeID) {
			if (item.IsSimpleText) {
				return item.Operator && item.Operator !== Operator[Operator.Populated] && item.Operator !== Operator[Operator.NotPopulated];
			}
			return true;
		} 
		if (item.CheckType === 'R') {
			return item.Operator && item.IntersectTypeID;
		}
		return false;
	}

	showThenValueOption(item) {
		if (item.FieldTypeID || item.FieldTypeName) {
			if (item.IsSimpleText) {				
				return item.Operator && item.Operator !== Operator[Operator.Populated] && item.Operator !== Operator[Operator.NotPopulated];				
			}			
			return true;
		}		
		return false;
	}
}
