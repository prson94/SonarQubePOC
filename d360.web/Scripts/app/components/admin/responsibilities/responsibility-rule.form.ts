import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { SelectItem, CheckboxModule } from 'primeng/primeng';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import {
    ResponsibilityType,
    ResponsibilityTypeRelation,
    ResponsibilityTypeRelationRule,
    ResponsibilityTypeRelationRuleDefinition,
    ResponsibilityTypeRelationRuleDefinitionWhenItem,
    ResponsibilityTypeRelationRuleDefinitionWhenTestRow,
    ResponsibilityTypeRelationRuleDefinitionThen,
    ResponsibilityTypeRelationRuleDefinitionThenItem,
    ResponsibilityTypeRelationRuleDefinitionThenTestRow,
    IResponsibilityTypeService,
    ResponsibilityTypeRelationRuleFormDataFieldType
} from '../../../models/responsibility-type.model';
import { MessagesService } from '../../../services/messages.service';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { BaseComponent } from '../../shared/base.component';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-responsibility-rule-form',
    templateUrl: './responsibility-rule.form.html',
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
            font-family: "Roboto", Tahoma !important;
            text-transform: uppercase;
            color: #5c5e60 !important;
            font-size: 1rem;
            font-weight: bold;
        }`
    ],
    providers: [ResponsibilityTypeService, ObjectDetailService],
})

export class ResponsibilityRuleForm extends BaseComponent implements OnInit, OnChanges {
    @Input() ruleId: number;
    @Input() id: number;

    @Output() onComplete = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private relations: ResponsibilityTypeRelation[] = [];
    private relation: ResponsibilityTypeRelation = new ResponsibilityTypeRelation();
    private model: ResponsibilityTypeRelationRule = new ResponsibilityTypeRelationRule();

    private actionName: string = "Add";
    //private lookupDefaultValueOptions: SelectItem[];

    //private testPattern: string;
    //private testPatternValidationText: string;
    //private syncApiNameWithName: boolean = true;

    //private relationItemCount = 0;

    //private childIntersectTypes: any[] = [];
    //private childIntersectsLoading = false;
    //private childIntersectDisabled = true;

    //private filteredLookup: string = '';
    //private filteredLookupDisplayFields: any[] = [];
    //private filteredSortOrderList: any[] = [];
    //private filteredLookupHideHeader: boolean = false;
    //private filteredLookupHideFooter: boolean = false; 
    //private selectedLookupToken = null;
    //private selectedFormatToken = null;
    private objectTypes: SelectItem[] = [];
    private whenCheckTypes: SelectItem[] = [
        { label: "Field", value: "F" },
        { label: "Relationship", value: "R" }
    ];
    private whenFieldTypes: ResponsibilityTypeRelationRuleFormDataFieldType[] = [];
    private whenIntersectTypes: SelectItem[] = [];
    private WhenTestRows: ResponsibilityTypeRelationRuleDefinitionWhenTestRow[] = [];
    private ThenTestRows: ResponsibilityTypeRelationRuleDefinitionThenTestRow[] = [];

    private thenObjectTypes: SelectItem[] = [
        { label: "Choose...", value: null },
        { label: "Group", value: "GroupType" },
        { label: "User", value: "ResourceType" }
    ];
    private thenFieldTypes: ResponsibilityTypeRelationRuleFormDataFieldType[] = [];

    //private supportsPrimaryFilterOption: boolean = false;
    //private displayFieldSelected: boolean = true;

    private errorMessage: string = "";

    constructor(private responsibilityTypeService: ResponsibilityTypeService, private messagesService: MessagesService, private objectDetailService: ObjectDetailService) {
        super();
        //if (!this.id) {
        //    this.model = new ResponsibilityTypeRelationRule();
        //    this.model.Definition = new ResponsibilityTypeRelationRuleDefinition();
        //    this.model.Definition.When = [];
        //    this.model.Definition.Then = new ResponsibilityTypeRelationRuleDefinitionThen();
        //    this.model.Definition.Then.Conditions = [];
        //}

        //this.model.FieldType = new FieldType();
        //this.model.FieldType.Object = this.objectType;
        //this.model.FieldType.ObjectID = this.objectID;
    }

    ngOnInit() {
        //this.initialItem = _.cloneDeep(this.model);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();
                //this.initialItem = _.cloneDeep(this.model);
            }
            else if (p == 'ruleId' && this.model != null) {
                this.load();
            }
            //else if (p == 'objectType') {
            //    this.supportsPrimaryFilterOption = (this.objectType && this.objectType.toLowerCase() == 'artifacttype');
            //}
        }        
    }

    //#region load functions

    private load(): void {
        if (this.id > 0) {
            this.actionName = 'Edit';
            this.isLoading = true;   
            this.responsibilityTypeService.getRelationOptionsByResponsibilityType(this.ruleId)
                .then(d => {
                    this.objectTypes = d;
                    this.objectTypes.unshift({ label: 'Choose...', value: null });
                })
                .then(() => {
                    let r: ResponsibilityTypeRelationRule;
                    this.responsibilityTypeService.getResponsibilityTypeRelationRule(this.id)
                        .then(data => {
                            this.model = data;
                            this.model.ObjectString = this.model.Object + '|' + this.model.ObjectID;
                            r = data;
                        })
                        .then(() => {
                            this.responsibilityTypeService.getRelationRuleFormData(this.model.Definition.Then.Object, this.model.Definition.Then.ObjectID)
                                .then(d => {
                                    this.thenFieldTypes = d.FieldTypes;
                                    this.thenFieldTypes.unshift({ label: 'Choose...', value: null, type: null, isLookup: false, values: [] });
                                })
                                .then(() => {
                                    this.responsibilityTypeService.getRelationRuleFormData(this.model.Object, this.model.ObjectID)
                                        .then(d => {
                                            this.whenFieldTypes = d.FieldTypes;

                                            this.model.Definition.When.forEach(wft => this.loadWhenValuesForFieldType(wft));

                                            this.whenIntersectTypes = d.IntersectTypes;
                                            this.whenFieldTypes.unshift({ label: 'Choose...', value: null, type: null, isLookup: false, values: [] });
                                            this.whenIntersectTypes.unshift({ label: 'Choose...', value: null });
                                        })
                                        .then(() => {
                                            this.model = r;
                                            this.model.ObjectString = r.Object + '|' + r.ObjectID;
                                            this.isLoading = false;
                                        });
                                });
                        });
                });
        } else {
            this.actionName = 'Add';
            this.isLoading = true;

            // Instantiate the object and its properties.
            this.model = new ResponsibilityTypeRelationRule();
            this.model.ResponsibilityTypeID = this.ruleId;
            this.model.Definition = new ResponsibilityTypeRelationRuleDefinition();
            this.model.Definition.When = [];
            this.model.Definition.Then = new ResponsibilityTypeRelationRuleDefinitionThen();
            this.model.Definition.Then.Conditions = [];

            this.responsibilityTypeService.getRelationOptionsByResponsibilityType(this.ruleId)
                .then(d => {
                    this.objectTypes = d;
                    this.objectTypes.unshift({ label: 'Choose...', value: null });
                })
            .then(() => {
                this.isLoading = false;
            });
        }
    }

    private loadObjectType(value: string): Promise<void> {
        let promises = [];
        if (value == null)
            return Promise.resolve();

        var otData = value.split("|");

        this.model.Object = otData[0];
        this.model.ObjectID = +otData[1];

        this.responsibilityTypeService.getRelationRuleFormData(this.model.Object, this.model.ObjectID)
            .then(d => {
                this.whenFieldTypes = d.FieldTypes;
                this.whenIntersectTypes = d.IntersectTypes;
                this.whenFieldTypes.unshift({ label: 'Choose...', value: null, type: null, isLookup: false, values: [] });
                this.whenIntersectTypes.unshift({ label: 'Choose...', value: null });
            })
            .then(() => {
                //this.isLoading = false;
            });

        return Promise.all(promises).then(() => { });
    }

    private addWhen(): void {
        let whenItem: ResponsibilityTypeRelationRuleDefinitionWhenItem = new ResponsibilityTypeRelationRuleDefinitionWhenItem();
        whenItem.CheckType = "F";
        this.model.Definition.When.push(whenItem);
    }

    private loadWhenValuesForFieldType(item: ResponsibilityTypeRelationRuleDefinitionWhenItem): Promise<void> {
        let selectedFieldType = this.whenFieldTypes.find(f => f.value == item.FieldTypeID.toString());
        item.Value = "";
        if (selectedFieldType) {
            item.FieldTypeName = selectedFieldType.label;
            if (selectedFieldType.isLookup) {
                selectedFieldType.values.unshift({ label: 'Choose...', value: null });
                item.ValueOptions = selectedFieldType.values;
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

    private testWhen(): Promise<void> {
        let promises = [];

        this.responsibilityTypeService.testWhen(this.model)
            .then(d => {
                this.WhenTestRows = d;
            })
            .then(() => {
                //this.isLoading = false;
            });

        return Promise.all(promises).then(() => { });
    }

    private testThen(): Promise<void> {
        let promises = [];

        this.responsibilityTypeService.testThen(this.model)
            .then(d => {
                this.ThenTestRows = d;
            })
            .then(() => {
                //this.isLoading = false;
            });

        return Promise.all(promises).then(() => { });
    }

    private addThenCondition(): void {
        let thenItem: ResponsibilityTypeRelationRuleDefinitionThenItem = new ResponsibilityTypeRelationRuleDefinitionThenItem();
        this.model.Definition.Then.Conditions.push(thenItem);
    }

    private loadThenFilterOptions(value: string): Promise<void> {
        let promises = [];

        if (value == null)
            return Promise.resolve();

        //var otData = value.split("|");

        this.model.Definition.Then.Object = value;//otData[0];
        this.model.Definition.Then.ObjectID = 1;//+otData[1];

        this.responsibilityTypeService.getRelationRuleFormData(this.model.Definition.Then.Object, this.model.Definition.Then.ObjectID)
            .then(d => {
                this.thenFieldTypes = d.FieldTypes;
                this.thenFieldTypes.unshift({ label: 'Choose...', value: null, type: null, isLookup: false, values: [] });
            })
            .then(() => {
                //this.isLoading = false;
            });

        return Promise.all(promises).then(() => { });
    }

    private removeThenCondition(i: number): void {
        this.model.Definition.Then.Conditions.splice(i, 1);
    }

    private loadThenValuesForFieldType(item: ResponsibilityTypeRelationRuleDefinitionWhenItem): Promise<void> {
        let selectedFieldType = this.thenFieldTypes.find(f => f.value == item.FieldTypeID.toString());
        item.Value = "";
        if (selectedFieldType) {
            item.FieldTypeName = selectedFieldType.label;
            if (selectedFieldType.isLookup) {
                selectedFieldType.values.unshift({ label: 'Choose...', value: null });
                item.ValueOptions = selectedFieldType.values;
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
        this.responsibilityTypeService.getRelationRuleFormDataRelationshipsForDropdown(this.model.Object, this.model.ObjectID, item.IntersectTypeID)
            .then(d => {
                item.ValueOptions = d;
            });
        return null;
    }

    //#endregion
    
    //#region form actions

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private onSubmit(): any {        
        //convert DisplayFields to objects
        //if (this.model.FusionItems) {
        //    this.model.FusionItems.forEach(i => {

        //        if (i.SourceFusionAttributeType.toString().indexOf('|') != -1)
        //            i.SourceFusionAttributeType = i.SourceFusionAttributeType.toString().split('|')[1];

        //        let d: FieldTypeFusionLookupDisplayField[] = [];

        //        (<string[]>i.DisplayFields).forEach(j => {
        //            let k = new FieldTypeFusionLookupDisplayField();
        //            try {
        //                k.FieldTypeID = parseInt(j.split('|')[0]);
        //                k.FieldTypeName = j.split('|')[1];
        //                k.Show = true;
        //            } catch (e) {
        //                return;
        //            }
        //            d.push(k);
        //        });

        //        i.DisplayFields = d;

        //    });
        //}

        //if (this.model.FieldType.Type == 'FilteredLookup') {
        //    let item = new FilteredLookupItem();
        //    item.Object = this.filteredLookup.split('|')[0];
        //    item.ObjectID = parseInt(this.filteredLookup.split('|')[1]);

        //    if (this.model.FilteredLookupItems != null) {
        //        item.ID = this.model.FilteredLookupItems[0].ID;
        //    }

        //    item.HideFooter = this.filteredLookupHideFooter;
        //    item.HideHeader = this.filteredLookupHideHeader;

        //    item.DisplayFields = [];
        //    this.filteredLookupDisplayFields.forEach(i => {
        //        item.DisplayFields.push({
        //            value: i.value,
        //            Filter: i.Filter,
        //            Show: i.Show,
        //            SortOrder: i.SortOrder,
        //            FieldTypeID: parseInt(i.value.split('|')[0]),
        //            FieldTypeName: i.value.split('|')[1]
        //        });
        //    });
        //    this.model.FilteredLookupItem = item;
        //}

        this.isLoading = true;
        if (this.model.ID > 0) {
            this.responsibilityTypeService.putRule(this.model)
                .then(r => {
                    this.isLoading = false;
                    this.showMessageForResult(this.messagesService, r);
                    if (r.type != 'error') {
                        this.onComplete.emit({ action: 'edit', field: this.model });
                    }
                });
        } else {
            this.responsibilityTypeService.postRule(this.model)
                .then(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.isLoading = false;
                    if (r.type != 'error') {                                                                
                        this.onComplete.emit({ action: 'add', field: this.model });
                    }
                });
        }
    }

    private validate(): boolean {
        let valid = true;
        this.errorMessage = '';

        if (!this.model.Name) {
            valid = false;
        }

        return valid;
    }

    //#endregion

    //#region dropdown functions

    //private changeRefType(index: number, selected: string = null): Promise<any> {
    //    let item = this.model.RelationItems[index];
    //    let last = (index == 0) ? null : this.model.RelationItems[index - 1];
    //    item.relationsLoading = true;
    //    item.DisplayFields = [];
    //    item.selectedRelationItemID = selected;
        
    //    let object = this.objectType;
    //    let objectId = this.objectID;

    //    if (index != 0) {
    //        object = last.Object;
    //        objectId = last.ObjectID;
    //    }

    //    switch (item.ReferenceType.toString()) {
    //        case ComplexLookupRelationType.ChildItem.toString(): //child item
    //            return this.fieldsService
    //                .getChildRelations(object, objectId)
    //                .then(ci => {
    //                    item.relationItems = ci;
    //                })
    //                .then(() => item.relationsLoading = false);
    //        case ComplexLookupRelationType.ChildRelationship.toString(): //child relationship
    //            let intersectIdToGetChildrenFor = item.IntersectType;
    //            if (last) {
    //                intersectIdToGetChildrenFor = last.IntersectType;
    //            }
    //            return this.fieldsService
    //                .getRelationLookupChildIntersectTypes(intersectIdToGetChildrenFor || 0)
    //                .then(ci => {
    //                    item.relationItems = ci;
    //                })
    //                .then(() => item.relationsLoading = false);
    //        case ComplexLookupRelationType.ParentItem.toString():
    //            return this.fieldsService
    //                .getParentRelations(object, objectId)
    //                .then(pi => {
    //                    item.relationItems = pi;
    //                })
    //                .then(() => item.relationsLoading = false);
    //        case ComplexLookupRelationType.StandardRelationhip.toString():
    //            return this.fieldsService
    //                .getStandardRelations(object, objectId)
    //                .then(sr => {
    //                    item.relationItems = sr;
    //                })
    //                .then(() => item.relationsLoading = false);
    //    }
    //}

    //private changeRel(index: number): Promise<any> {
    //    let item = this.model.RelationItems[index];
    //    let last = (index == 0) ? null : this.model.RelationItems[index - 1];
        
    //    let params = [];
    //    if (item.selectedRelationItemID) {
    //        params = item.selectedRelationItemID.split('|');
    //    } else {
    //        params.push(item.IntersectType);
    //        params.push(item.Object);
    //        params.push(item.ObjectID);
    //        item.selectedRelationItemID = item.IntersectType + '|' + item.Object + '|' + item.ObjectID;
    //    }

    //    try {
    //        if (params.length < 3)
    //            return;
    //        let id = parseInt(params[2]);
    //        let type = params[1];
    //        let intersectType = parseInt(params[0]);

    //        item.IntersectType = intersectType;
    //        item.Object = type;
    //        item.ObjectID = id;

    //        item.DisplayFields = [];
    //        return this.fieldsService.getRelationLookupDisplayFields(id, type, intersectType)
    //            .then(r => {
    //                r.forEach(i => {
    //                    let params = i.value.split('|');
    //                    let d = new FieldTypeItemDisplayFieldEditorModel();
    //                    d.FieldTypeID = parseInt(params[0]);
    //                    d.FieldTypeName = params[1];
    //                    d.Show = false;
    //                    d.FilterValue = "";
    //                    d.SortOrder = null;
    //                    d.value = i.value;
    //                    let e = item.DisplayFields.find(j => j.FieldTypeID == d.FieldTypeID && j.FieldTypeName == d.FieldTypeName);
    //                    if (e != null) {
    //                        e.Show = true;
    //                        e.value = i.value;
    //                    } else
    //                        item.DisplayFields.push(d);
    //                });

    //                let s = [];
    //                for (let i = 1; i <= item.DisplayFields.length; i++) {
    //                    item.DisplayFields[i - 1].DisplayOrder = i;
    //                    s.push({ id: i, text: i });
    //                }
    //                item.SortOrderList = s;

    //            });

    //    } catch (e) {
    //        return Promise.resolve();
    //    }
    //}

    //private changeDisplayOrder(item: FieldTypeItemDisplayFieldEditorModel, parent: FieldTypeRelationItemEditorModel) {
    //    let other = parent.DisplayFields.find(f => f.DisplayOrder == item.DisplayOrder && f.value != item.value);
    //    if (other) {
    //        let sum = (parent.DisplayFields.length * (parent.DisplayFields.length + 1)) / 2;
    //        let total = _.sumBy(parent.DisplayFields, i => { return (i == other) ? 0 : (+i.DisplayOrder || 0); });
    //        other.DisplayOrder = sum - total;
    //    }
            
    //}

    //private changeLegacyRef(): Promise<any> {

    //    this.childIntersectDisabled = (this.model.RelationItem.ReferenceType.toString() || '1') == '1';
    //    this.model.RelationItem.DisplayFields = [];
    //    if (this.model.RelationItem.selectedRelationItemID != null) {
    //        let params = this.model.RelationItem.selectedRelationItemID.split('|');

    //        this.model.RelationItem.IntersectType = parseInt(params[0]);
    //        this.model.RelationItem.Object = params[1];
    //        this.model.RelationItem.ObjectID = parseInt(params[2]);
    //    }

    //    if (this.model.RelationItem.IntersectType != null && !this.childIntersectDisabled) {
    //        this.childIntersectsLoading = true;
    //        return this.fieldsService.getRelationLookupChildIntersectTypes(this.model.RelationItem.IntersectType)
    //            .then(r => {
    //                this.childIntersectTypes = r;
    //                this.childIntersectsLoading = false;
    //            });
    //    } else if (this.childIntersectDisabled) {
    //        return this.changeLegacyChild();
    //    } else return Promise.resolve();
    //}

    //private changeLegacyChild(): Promise<any> {

    //    let intersectType = this.model.RelationItem.IntersectType;
    //    let type = this.model.RelationItem.Object;
    //    let id = this.model.RelationItem.ObjectID;

    //    if (this.model.RelationItem.ReferenceType.toString() != '1') { //not self ref 
    //        let params = this.model.RelationItem.selectedChildIntersectType.split('|');
    //        intersectType = parseInt(params[0]);
    //        type = params[1];
    //        id = parseInt(params[2]);
    //    }

    //    if (intersectType && id && type) {
    //        let item = this.model.RelationItem;
    //        item.DisplayFields = [];
    //        return this.fieldsService.getRelationLookupDisplayFields(id, type, intersectType)
    //            .then(r => {
    //                r.forEach(i => {
    //                    let params = i.value.split('|');
    //                    let d = new FieldTypeItemDisplayFieldEditorModel();
    //                    d.FieldTypeID = parseInt(params[0]);
    //                    d.FieldTypeName = params[1];
    //                    d.Show = false;
    //                    d.FilterValue = "";
    //                    d.SortOrder = null;
    //                    d.value = i.value;
    //                    let e = item.DisplayFields.find(j => j.FieldTypeID == d.FieldTypeID && j.FieldTypeName == d.FieldTypeName);
    //                    if (e != null) {                            
    //                        e.Show = true;
    //                        e.value = i.value;
    //                    } else
    //                        item.DisplayFields.push(d);
    //                });

    //                let s = [];
    //                for (let i = 1; i <= item.DisplayFields.length; i++) {
    //                    item.DisplayFields[i - 1].DisplayOrder = i;
    //                    s.push({ id: i, text: i });
    //                }
    //                item.SortOrderList = s;
    //            });
    //    } else return Promise.resolve();
    //}

    //private changeFilteredLookup(): Promise<any> {
    //    if (this.filteredLookup == null || this.filteredLookup == '') {
    //        this.filteredLookupDisplayFields = [];
    //        return Promise.resolve();
    //    }
    //    let params = this.filteredLookup.split('|');
    //    let id = parseInt(params[1]);
    //    let type = params[0];


    //    return this.fieldsService.getFilteredLookupDisplayFields(this.objectType, this.objectID, type, id)
    //        .then(d => {
    //            this.filteredLookupDisplayFields = d;

    //            this.filteredSortOrderList = [];
    //            for (let i = 0; i < this.filteredLookupDisplayFields.length; i++) {
    //                this.filteredSortOrderList.push({
    //                    id: i + 1,
    //                    text: i + 1
    //                });
    //            }                
    //        });
    //}

    //#endregion

    //private selectDisplayToken(value: string) {
    //    if (value == null || value == '' || value == 'null')
    //        return;
    //    if (this.model.FieldType.LookupDisplayFormat == null) {
    //        this.model.FieldType.LookupDisplayFormat = '';
    //    }
    //    this.selectedLookupToken = null;
    //    this.model.FieldType.LookupDisplayFormat += value;
    //}

    //private selectEditToken(value: string) {
    //    if (value == null || value == '' || value == 'null')
    //        return;
    //    if (this.model.FieldType.LookupEditFormat == null) {
    //        this.model.FieldType.LookupEditFormat = '';
    //    }
    //    this.selectedFormatToken = null;
    //    this.model.FieldType.LookupEditFormat += value;
    //}

    //private validatePattern() {
    //    if (this.model.FieldType.Pattern > "" && this.testPattern > "") {
    //        var patternRegex = new RegExp(this.model.FieldType.Pattern);
    //        this.testPatternValidationText = (patternRegex.test(this.testPattern)) ? 'Success' : 'Fail';
    //    }
    //    else {
    //        this.testPatternValidationText = '';
    //    }
    //}

    //private updateApiName(event) {
    //    this.model.FieldType.Name = event.target.value.replace(/[^a-zA-Z0-9-_]/g, '');
    //}

    //private addFusion() {
    //    let i = new FieldTypeFusionItemEditorModel();
    //    i.ReferenceType = this.lookups.ReferenceTypes[0].value;
    //    if (this.model.FusionItems == null) {
    //        this.model.FusionItems = [];
    //    }
    //    this.model.FusionItems.push(i);
    //}

    //private removeFusion(i: number) {
    //    this.model.FusionItems.splice(i, 1);
    //}

    //private addRelation(item: FieldTypeRelationItemEditorModel) {
    //    let i = new FieldTypeRelationItemEditorModel();
    //    let params = item.selectedRelationItemID.split('|');
    //    let id = parseInt(params[2]);
    //    let type = params[1];
    //    let intersectType = parseInt(params[0]);

    //    i.ObjectID = id;
    //    i.Object = type;
    //    i.IntersectTypeID = intersectType;
    //    i.IntersectType = intersectType;
    //    i.displayValue = item.relationItems.find(i => i.value == item.selectedRelationItemID).title;

    //    this.model.RelationItems.push(i);
    //    this.relationItemCount = this.model.RelationItems.length;
    //}

    //private removeRelation(item: FieldTypeRelationItemEditorModel) {
    //    //only last item can be deleted
    //    this.model.RelationItems.pop();
    //    this.relationItemCount = this.model.RelationItems.length;
    //}

    //private anyDisplayFieldsSelected(e: any) {
    //    if (this.model.FieldType.Type != 'ComplexRelationLookup') {
    //        this.displayFieldSelected = true;
    //        return;
    //    }
    //    if (e == true) {
    //        this.displayFieldSelected = true;
    //        return;
    //    }
    //    this.displayFieldSelected = false;
    //    this.model.RelationItems.forEach(r => {
    //        r.DisplayFields.forEach(d => {
    //            if (d.Show) {
    //                this.displayFieldSelected = true;
    //                return;
    //            }
    //        });
    //    });
    //}
}