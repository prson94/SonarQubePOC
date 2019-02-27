import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { SelectItem, CheckboxModule } from 'primeng/primeng';
import {
    FieldType, 
    FieldTypeEditorModel,
    FilteredLookupItem,
    FilteredLookupDisplayField,
    Lookups,    
    FieldTypeFusionItemEditorModel,
    OwnershipLookupSettings,
    FieldTypeFusionLookupDisplayField,
    FieldTypeRelationItemEditorModel,
    ComplexLookupRelationType,
    FieldTypeItemDisplayFieldEditorModel, } from '../../../models/fields.model';
import { FieldsService } from '../../../services/fields.service';
import { MessagesService } from '../../../services/messages.service';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { BaseComponent } from '../../shared/base.component';

import * as _ from 'lodash';
import { createWriteStream } from 'fs';
import { FormHelper } from '../../../models/form.model';
import { FormHelpers } from '../../../static/form-helpers';

@Component({
    selector: 'd3s-field-type-form',
    templateUrl: './field-type.form.html',
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
    providers: [FieldsService, ObjectDetailService],
})

export class FieldTypeForm extends BaseComponent implements OnInit, OnChanges {
    @Input() id: number;
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() actionName: string = "Add";
    @Input() objectName: string = '';   
    @Output() onComplete = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    @Input() showIsListable: boolean = true;
    @Input() showIsPartOfKey: boolean = true;

    private lookups: Lookups = new Lookups();
    private lookupDefaultValueOptions: SelectItem[];
    private booleanDefaultValueOptions: SelectItem[];
    private model: FieldTypeEditorModel;    
    private initialItem: FieldTypeEditorModel;

    private testPattern: string;
    private testPatternValidationText: string;
    private syncApiNameWithName: boolean = true;

    private relationItemCount = 0;

    private childIntersectTypes: any[] = [];
    private childIntersectsLoading = false;
    private childIntersectDisabled = true;

    private filteredLookup: string = '';
    private filteredLookupDisplayFields: any[] = [];
    private filteredSortOrderList: any[] = [];
    private filteredLookupHideHeader: boolean = false;
    private filteredLookupHideFooter: boolean = false; 
    private selectedLookupToken = null;
    private selectedFormatToken = null;
    private fieldsFromRelation: SelectItem[] = [];

    private supportsPrimaryFilterOption: boolean = false;
    private displayFieldSelected: boolean = true;    
    public listParentFields: SelectItem[] = [];

    private errorMessage: string = "";
    private isListableRelationship: boolean = false;

    public defaultDate: any;
    public defaultLinkName: any;
    public defaultLinkAdress: any;

    constructor(private fieldsService: FieldsService, private messagesService: MessagesService, private objectDetailService: ObjectDetailService) {
        super();
        this.model = new FieldTypeEditorModel();
        this.model.FieldType = new FieldType();
        this.model.FieldType.Object = this.objectType;
        this.model.FieldType.ObjectID = this.objectID;
        this.booleanDefaultValueOptions = [
            { label: '-No Default-', value: null },
            { label: 'True', value: 'true' },
            { label: 'False', value: 'false' },
        ]
    }

    ngOnInit() {
        this.initialItem = _.cloneDeep(this.model);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();
                this.initialItem = _.cloneDeep(this.model);
            }
            else if (p == 'objectID' && this.model.FieldType != null) {
                this.model.FieldType.Object = this.objectType;
                this.model.FieldType.ObjectID = this.objectID;
            }
            else if (p == 'objectType') {
                this.supportsPrimaryFilterOption = (this.objectType && this.objectType.toLowerCase() == 'artifacttype');
            }
        }        
    }

    //#region load functions

    private load(): void {
        if (this.id > 0) {
            this.actionName = 'Edit';
            this.isLoading = true;                        
            this.fieldsService.getFieldTypeEditor(this.id)
                .then(data => {                    
                    this.model = data;
                    this.model.cardinalRelationship = null;
                    this.model.selectedLookup = null;
                    switch (this.model.FieldType.Type) {
                        case "Lookup":
                            if (this.model.FieldType.LookupObjectType != null && this.model.FieldType.LookupObjectID != null)
                                this.model.selectedLookup = this.model.FieldType.LookupObjectType + '|' + this.model.FieldType.LookupObjectID;
                            break;
                        case "Relationship":
                            if (this.model.FieldType.LookupObjectType != null && this.model.FieldType.LookupObjectID != null)
                                this.model.cardinalRelationship = this.model.FieldType.LookupObjectID;
                            break;
                        case "FieldFromRelationship":
                            if (this.model.FieldType.LookupObjectType != null && this.model.FieldType.LookupObjectID != null)
                                this.model.cardinalRelationship = this.model.FieldType.LookupObjectID;
                            break;
                        case "RefListRelationship":
                            if (this.model.FieldType.LookupObjectType != null && this.model.FieldType.LookupObjectID != null)
                                this.model.cardinalRelationship = this.model.FieldType.LookupObjectID;
                            break;
                    }
                })
                .then(() => this.fieldsService.getLookups(this.model.FieldType.ObjectID, this.model.FieldType.Object))
                .then(d => {                    
                    this.lookups = d;
                    this.lookups.IntersectTypes.forEach(i => {
                        i.id = i.value.split('|')[0];
                    });

                    this.lookups.ReferenceTypes = this.fieldsService.getReferenceTypes();
                })
                .then(() => { if (this.id > 0) return this.fieldsService.getFormData(this.id) })
                .then(f => {
                    if (f) {                        
                        this.model.OwnershipLookupSettings = f.OwnershipLookupSettings;
                        this.model.RelationItems = f.RelationItems;
                        this.model.FusionItems = f.FusionItems;
                        if (this.model.FusionItems != null)
                            this.model.FusionItems.forEach(i => {
                                if (i.SourceFusionAttributeType.toString().indexOf('|') == -1)
                                    i.SourceFusionAttributeType = 'FusionAttributeType|' + i.SourceFusionAttributeType.toString();

                                for (let j = 0; j < i.DisplayFields.length; j++) {
                                    let d = i.DisplayFields[j] as FieldTypeFusionLookupDisplayField;
                                    i.DisplayFields[j] = d.value;
                                }
                                
                            });

                        this.model.FilteredLookupItems = f.FilteredLookupItems;

                        if (this.model.RelationItems && this.model.FieldType.Type == 'ComplexRelationLookup') {
                            this.loadComplexRelationLookup();
                        }
                    }
                })
                .then(() => {
                    return this.loadDataType(this.model.FieldType.Type);
                })
                .then(() => {           
                    this.isLoading = false;
                });
        } else {
            this.actionName = 'Add';
            this.isLoading = true;
            this.model = new FieldTypeEditorModel();
            this.model.FieldType = new FieldType();
            //set boolean defaults;
            this.model.FieldType.IsDisplayable = true;
            this.model.FieldType.IsEditable = true;
            this.model.FieldType.IsListable = false;
                        
            this.model.OwnershipLookupSettings = new OwnershipLookupSettings();
            this.model.OwnershipLookupSettings.DisplayAssignmentSource = false;
            this.model.OwnershipLookupSettings.ExpandGroupMembership = true;

            this.fieldsService.getLookups(this.objectID, this.objectType)
                .then(d => {                    
                    this.lookups = d;
                    this.lookups.ReferenceTypes = this.fieldsService.getReferenceTypes()
                    this.lookups.DataTypes.unshift({ label: 'Choose...', value: null });
                    this.model.FieldType.Type = null;
                })
                .then(() => {
                    this.isLoading = false;
                });
        }
    }

    private loadComplexRelationLookup() {
        //load existing values
        this.model.RelationItems.forEach(r => {
            
            let intersectType = this.lookups.IntersectTypes.find(i => i.id == r.IntersectType.toString());

            if (r.Object == null || r.Object == '')
                r.Object = intersectType.value.split('|')[1];
            if (r.ObjectID == null || r.ObjectID < 0)
                r.ObjectID = parseInt(intersectType.value.split('|')[2]);

            r.DisplayFields.forEach(d => {
                if (d.FieldTypeID == null && d.value)
                    d.FieldTypeID = parseInt(d.value.split('|')[0]);
                if (d.FieldTypeName == null && d.value)
                    d.FieldTypeName = d.value.split('|')[1];

                if (!d.value)
                    d.value = d.FieldTypeID + '|' + d.FieldTypeName;
            });

        });


        let clone = _.cloneDeep(this.model.RelationItems);
        if (this.model.RelationItems != null && this.model.RelationItems.length) {
            for (let i = 0; i < this.model.RelationItems.length; i++) {
                let item = this.model.RelationItems[i];
                let last = (i == 0) ? null : this.model.RelationItems[i - 1];

                if (i == 0) {
                    this.objectDetailService.getObject(this.objectID, this.objectType)
                        .then(o => {
                            this.objectName = o.Name;
                        });
                }
                
                //load cascading dropdowns
                this.changeRefType(i)
                    .then(() => {
                        item.selectedRelationItemID = item.IntersectType + '|' + item.Object + '|' + item.ObjectID + '|' + item.Direction;
                    })
                    .then(() => this.changeRel(i))
                    .then(() => {
                        let parent = item;
                        item.DisplayFields.forEach(d => {
                            let item = clone[i].DisplayFields.find(f => f.FieldTypeID == d.FieldTypeID && f.FieldTypeName == d.FieldTypeName);

                            if (item) {
                                d.Show = (item.Show == null) ? true : item.Show;
                                d.DisplayOrder = item.DisplayOrder;
                                d.FilterValue = item.Filter;
                                d.OverrideDisplayName = item.OverrideDisplayName;
                                d.SortOrder = item.SortOrder;
                                d.Width = item.Width;

                                if (d.DisplayOrder != null)
                                    this.changeDisplayOrder(item, parent); 
                            }

                        });

                        let r = item.relationItems.find(f => f.value == item.selectedRelationItemID);
                        if (i > 0)
                            r = this.model.RelationItems[i - 1].relationItems.find(f => f.value == this.model.RelationItems[i - 1].selectedRelationItemID);
                        if (r) item.displayValue = r.title;
                    });

                //load display order/sort order drop down lists
                this.model.RelationItems.forEach(r => {
                    let s = [];
                    for (let i = 1; i <= r.DisplayFields.length; i++) {
                        r.DisplayFields[i - 1].DisplayOrder = i;
                        s.push({ id: i, text: i });
                    }
                    r.SortOrderList = s;

                });
                this.relationItemCount = this.model.RelationItems.length;
            }
        }
    }

    private loadDataType(value: string): Promise<void> {
        
        let promises = [];
        if (value == null)
            return Promise.resolve();

        switch (value.toLowerCase()) {
            case 'lookup':
                promises.push(this.lookupTypeSelected(this.model.selectedLookup || this.lookups.Lookups[0].value));
                break;
            case 'relationship':
                try {
                    if (this.model.cardinalRelationship) {
                        promises.push(this.cardinalRelationshipSelected(this.model.cardinalRelationship));
                    }
                    else if (this.lookups.Field_Relationships.length > 0) {
                        promises.push(this.cardinalRelationshipSelected(this.lookups.Field_Relationships[0].value));
                    }                    
                } catch (e) {
                    console.log(e);
                }
                break;
            case 'fieldfromrelationship':
                try {
                    if (this.model.cardinalRelationship) {
                        promises.push(this.cardinalFieldFromRelationshipSelected(this.model.cardinalRelationship, this.model.FieldType.LookupObjectFieldTypeID));
                    }
                    else if (this.lookups.Field_CardinalRelationships.length > 0) {
                        promises.push(this.cardinalFieldFromRelationshipSelected(this.lookups.Field_FieldFromRelRelationships[0].value, this.model.FieldType.LookupObjectFieldTypeID));

                    }
                } catch (e) {
                    console.log(e);
                }
                break;
            case 'reflistrelationship':
                try {
                    this.model.FieldType.IsListable = false;
                    if (this.model.cardinalRelationship && (this.lookups.Field_CardinalReferenceRelationships.length > 0)
                        && (this.lookups.Field_CardinalReferenceRelationships.find(x => x.value == this.model.cardinalRelationship))) {
                        promises.push(this.cardinalFieldFromRelationshipSelected(this.model.cardinalRelationship));
                    }
                    else if (this.lookups.Field_CardinalReferenceRelationships.length > 0) {
                        promises.push(this.cardinalFieldFromRelationshipSelected(this.lookups.Field_CardinalReferenceRelationships[0].value));
                    }
                    else {
                        this.model.FieldType.LookupObjectID = null;
                        this.model.FieldType.LookupObjectType = null;
                    }
                } catch (e) {
                    console.log(e);
                }
                break;
            case 'fusionlookup':
                this.model.FieldType.IsListable = false;
                this.model.FieldType.IsEditable = false;                
                this.model.FieldType.IsRequired = false;
                this.model.FieldType.LookupDisplayFormat = null;
                this.lookups.ReferenceTypes = this.fieldsService.getFusionReferenceTypes();
                if (this.model.FusionItems && this.model.FusionItems.length)
                    this.model.FusionItems.forEach(i => {
                        promises.push(
                            this.loadTargetFusionAttributes(i)
                                .then(() => this.loadFusionDisplayFields(i))
                        );
                    });
                break;
            case 'complexrelationlookup':
                this.model.FieldType.IsEditable = false;
                this.model.FieldType.IsListable = false;
                this.model.FieldType.IsPartOfKey = false;
                this.model.FieldType.IsRequired = false;
                this.model.FieldType.LookupDisplayFormat = null;                
                if (this.model.RelationItems == null || this.model.RelationItems.length == 0) {
                    let r = new FieldTypeRelationItemEditorModel();
                    r.DisplayFields = [];
                    r.ReferenceType = 1;
                    r.Object = this.objectType;
                    r.ObjectID = this.objectID;
                    this.model.RelationItems = [];
                    this.model.RelationItems.push(r);
                    this.relationItemCount = 1;

                    if (this.objectName == null || this.objectName == '') {
                        this.objectDetailService.getObject(this.objectID, this.objectType).then(o => {
                            this.objectName = o.Name;
                        });

                    }
                    this.changeRefType(this.model.RelationItems.length - 1);
                }
                break;
            case 'filteredlookup':
                this.model.FieldType.IsEditable = false;
                this.model.FieldType.IsListable = false;
                this.model.FieldType.IsPartOfKey = false;
                this.model.FieldType.IsRequired = false;
                this.model.FieldType.LookupDisplayFormat = null;                
                this.loadFilteredLookup();
                break;
            case 'ownershiplookup':
                if (!this.model.OwnershipLookupSettings) this.model.OwnershipLookupSettings = new OwnershipLookupSettings(); 
                this.model.FieldType.IsEditable = false;
                this.model.FieldType.IsListable = false;
                this.model.FieldType.IsPartOfKey = false;
                this.model.FieldType.IsRequired = false;        
                this.model.FieldType.LookupDisplayFormat = null;        
                break;
            default:       
                this.model.FieldType.LookupDisplayFormat = null;  
                this.model.FieldType.LookupObjectID = null;
                this.model.FieldType.LookupObjectType = null;
                break;
        }
        if (this.model.FieldType.Type == 'Date' && this.model.FieldType.DefaultValue != null) {
            this.defaultDate = new Date(this.model.FieldType.DefaultValue);
        }

        if (this.model.FieldType.Type == 'Link' && this.model.FieldType.DefaultValue != null) {
            var link = this.model.FieldType.DefaultValue.split('|');
            this.defaultLinkName = link[0];
            this.defaultLinkAdress = link[1];
        }
        return Promise.all(promises).then(() => {
        });
    }

    // called when the lookup type field is changed
    private lookupTypeSelected(value: string): Promise<any> {
        if (value == undefined) {
            console.log("[ERROR] - LOOKUP TYPE IS UNDEFINED", value);
            return Promise.resolve();
        }

        //update the model to have correct lookuptype object and id
        let id = parseInt(value.split('|')[1]);
        let type = value.split('|')[0];
                
        if (this.model.FieldType.LookupObjectID != id || this.model.FieldType.LookupObjectType != type) {
            this.model.FieldType.LookupDisplayFormat = "";
            this.model.FieldType.LookupEditFormat = "";
        }

        this.model.FieldType.LookupObjectID = id;
        this.model.FieldType.LookupObjectType = type;
        
        this.loadDefaultValueOptions(type, id);
        this.loadHierarchyOptions(type, id);

        //clear the validated fields and error message
        this.model.FieldType.MaximumLength = null;
        this.model.FieldType.MinimumLength = null;
        this.model.FieldType.Increment = null;
        this.validateNumber(this.model.FieldType.Type);
        

        return this.loadTokens(type, id);
    }

    // called when the lookup type field is changed
    private cardinalRelationshipSelected(value: number): Promise<any> {
        if (value == undefined) {
            console.log("[ERROR] - Intersect TYPE IS UNDEFINED", value);
            return Promise.resolve();
        }
        this.isListableRelationship = false;

        this.fieldsService.getRelationshipFieldIsListable(this.objectType, this.objectID, value).then(res => {
            this.isListableRelationship = res;
            if (!this.isListableRelationship)
                this.model.FieldType.IsListable = false;
        })
        

        //update the model to have correct lookuptype object and id
        this.model.FieldType.LookupObjectID = value;
        this.model.FieldType.LookupObjectType = "IntersectType";
    }

    private cardinalFieldFromRelationshipSelected(value: number, fieldTypeId: number = null): Promise<any> {
        if (value == undefined) {
            console.log("[ERROR] - Intersect TYPE IS UNDEFINED", value);
            return Promise.resolve();
        }

        //update the model to have correct lookuptype object and id
        this.model.FieldType.LookupObjectID = value;
        this.model.FieldType.LookupObjectType = "IntersectType";

        this.fieldsService.getRelationObjectFields(this.objectType, this.objectID, value)
            .then(d => {
                this.fieldsFromRelation = d;
                if (fieldTypeId != null)
                    this.model.FieldType.LookupObjectFieldTypeID = fieldTypeId
                else if (this.fieldsFromRelation.length > 0)
                    this.model.FieldType.LookupObjectFieldTypeID = this.fieldsFromRelation[0].value;
                else
                    this.model.FieldType.LookupObjectFieldTypeID = null;
            });    
    }

    private cardinalReferenceItemListFromRelationshipSelected(value: number): Promise<any> {
        if (value == undefined) {
            console.log("[ERROR] - Intersect TYPE IS UNDEFINED", value);
            return Promise.resolve();
        }

        //update the model to have correct lookuptype object and id
        this.model.FieldType.LookupObjectID = value;
        this.model.FieldType.LookupObjectType = "IntersectType";
    }

    private cardinalFieldFromRelationship_FieldSelected(value: number): Promise<any> {
        this.model.FieldType.LookupObjectFieldTypeID = value;        
        return Promise.resolve();
    }

    private loadHierarchyOptions(objectType: string, objectId: number): void {
        this.listParentFields = [];
        if (objectType != 'ReferenceItem') {
            if (this.model != null && this.model.FieldType != null)
                this.model.FieldType.ParentFieldTypeID = 0;
            return;
        }
        
        this.fieldsService.getReferenceTypeHierarchyFields(objectId, this.objectType, this.objectID).then(r => {
            this.listParentFields = r;
            if (this.listParentFields == null || this.listParentFields.length == 0) {
                this.model.FieldType.ParentFieldTypeID = 0;
            }
        });
    }

    private loadDefaultValueOptions(objectType: string, objectId: number): Promise<void> {
        if (this.model.FieldType.LookupObjectType == undefined || this.model.FieldType.LookupObjectID == undefined) {
            console.log("[ERROR] - NO TYPE OR ID SPECIFIED TO LOAD DEFAULT VALUES FOR", this.model.FieldType.LookupObjectID, this.model.FieldType.LookupObjectType);
            return;
        }

        if (objectType != "DomainItem" && objectType != "ReferenceItemType" && objectType != "TaxonomyType") objectType += 'Type';
        
        return this.fieldsService.getLookupDefaultValueOptions(objectId, objectType)
            .then(r => {
                this.lookupDefaultValueOptions = r;
            });
    }

    private loadTokens(objectType: string, objectId: number): Promise<void> {
        if (this.model.FieldType.LookupObjectType == undefined || this.model.FieldType.LookupObjectID == undefined) {
            console.log("[ERROR] - NO TYPE OR ID SPECIFIED TO LOAD TOKENS FOR", this.model.FieldType.LookupObjectID, this.model.FieldType.LookupObjectType);
            return;
        }

        if (objectType != "DomainItem" && objectType != "ReferenceItemType" && objectType != "TaxonomyType") objectType += 'Type';


        return this.fieldsService.getLookupTokens(objectId, objectType)
            .then(r => {
                this.model.LookupTokens = r;             
                if (this.model.LookupTokens
                    && this.model.LookupTokens.length > 0
                    && (this.model.FieldType.LookupDisplayFormat == null 
                    || this.model.FieldType.LookupDisplayFormat.length == 0))
                    this.model.FieldType.LookupDisplayFormat = this.model.LookupTokens[0].value;
            });
    }

    private loadTargetFusionAttributes(item: FieldTypeFusionItemEditorModel): Promise<void> {
        let id;
        if (item.SourceFusionAttributeType == null)
            return;
        if (item.SourceFusionAttributeType.toString().indexOf('|') != -1)
            id = item.SourceFusionAttributeType.split('|')[1];
        else
            id = item.SourceFusionAttributeType;

        return this.fieldsService.getFusionLookupTargetAttributeTypes(+id, item.ReferenceType)
            .then(d => {
                item.TargetFusionAttributeTypes = d;
            });
    }

    private loadFusionDisplayFields(item: FieldTypeFusionItemEditorModel): Promise<void> {
        return this.fieldsService.getFusionDisplayFields(+item.TargetFusionAttributeType || +item.SourceFusionAttributeType)
            .then(d => {
                item.FusionDisplayFields = d;
            });
    }

    private loadFilteredLookup() {
        if (this.model.FilteredLookupItems == null || this.model.FilteredLookupItems.length < 1)
            return;
        let item = this.model.FilteredLookupItems[0];

        this.filteredLookup = item.Object + '|' + item.ObjectID;
        this.filteredLookupHideHeader = item.HideHeader;
        this.filteredLookupHideFooter = item.HideFooter;

        this.changeFilteredLookup()
            .then(() => {
                this.filteredLookupDisplayFields.forEach(d => {
                    let i = item.DisplayFields.find(j => j.value == d.value);
                    if (i) {
                        d.Show = i.Show;
                        d.Filter = i.Filter;
                        d.SortOrder = i.SortOrder;
                    }
                });
            });
    }
    //#endregion
    
    //#region form actions

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private onSubmit(): any {  
        //convert DisplayFields to objects
        if (this.model.FusionItems) {
            this.model.FusionItems.forEach(i => {

                if (i.SourceFusionAttributeType.toString().indexOf('|') != -1)
                    i.SourceFusionAttributeType = i.SourceFusionAttributeType.toString().split('|')[1];

                let d: FieldTypeFusionLookupDisplayField[] = [];

                (<string[]>i.DisplayFields).forEach(j => {
                    let k = new FieldTypeFusionLookupDisplayField();
                    try {
                        k.FieldTypeID = parseInt(j.split('|')[0]);
                        k.FieldTypeName = j.split('|')[1];
                        k.Show = true;
                    } catch (e) {
                        return;
                    }
                    d.push(k);
                });

                i.DisplayFields = d;

            });
        }
        
        if (this.model.FieldType.Type == 'FilteredLookup') {
            let item = new FilteredLookupItem();
            item.Object = this.filteredLookup.split('|')[0];
            item.ObjectID = parseInt(this.filteredLookup.split('|')[1]);

            if (this.model.FilteredLookupItems != null) {
                item.ID = this.model.FilteredLookupItems[0].ID;
            }

            item.HideFooter = this.filteredLookupHideFooter;
            item.HideHeader = this.filteredLookupHideHeader;

            item.DisplayFields = [];
            this.filteredLookupDisplayFields.forEach(i => {
                item.DisplayFields.push({
                    value: i.value,
                    Filter: i.Filter,
                    Show: i.Show,
                    SortOrder: i.SortOrder,
                    FieldTypeID: parseInt(i.value.split('|')[0]),
                    FieldTypeName: i.value.split('|')[1]
                });
            });
            this.model.FilteredLookupItem = item;
        }

        if (this.model.FieldType.Type == 'Link') {
            {
                this.model.FieldType.DefaultValue = this.defaultLinkName != null ? this.defaultLinkName : '';// + '|' + this.defaultLinkAdress != null ? this.defaultLinkAdress : '';
                this.model.FieldType.DefaultValue += '|';
                this.model.FieldType.DefaultValue += this.defaultLinkAdress != null ? this.defaultLinkAdress : '';
            }
        }
        this.isLoading = true;
        if (this.model.FieldType.ID > 0) {
            this.fieldsService.putFieldType(this.model)
                .then(r => {
                    this.isLoading = false;
                    this.showMessageForResult(this.messagesService, r);
                    if (r.type != 'error') {
                        this.onComplete.emit({ action: 'edit', field: this.model });
                    }
                });
        } else {
            this.fieldsService.postFieldType(this.model)
                .then(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.isLoading = false;
                    if (r.type != 'error') {                                                                
                        this.onComplete.emit({ action: 'add', field: this.model });
                    }
                });
        }
    }

    private valid(): boolean {
        let valid = true;

        if (this.model.FieldType.Type == 'RefListRelationship' && !this.model.FieldType.LookupObjectID)
            valid = false;

        if (this.model.FieldType.Type == 'FieldFromRelationship' && !this.model.FieldType.LookupObjectFieldTypeID)
            valid = false;

        return valid;
    }

    //#endregion

    //#region dropdown functions

    private changeRefType(index: number, selected: string = null): Promise<any> {
        let item = this.model.RelationItems[index];
        let last = (index == 0) ? null : this.model.RelationItems[index - 1];
        item.relationsLoading = true;
        item.DisplayFields = [];
        item.selectedRelationItemID = selected;
        
        let object = this.objectType;
        let objectId = this.objectID;

        if (index != 0) {
            object = last.Object;
            objectId = last.ObjectID;
        }

        switch (item.ReferenceType.toString()) {
            case ComplexLookupRelationType.ChildItem.toString(): //child item
                return this.fieldsService
                    .getChildRelations(object, objectId)
                    .then(ci => {
                        item.relationItems = ci;
                    })
                    .then(() => item.relationsLoading = false);
            case ComplexLookupRelationType.ChildRelationship.toString(): //child relationship
                let intersectIdToGetChildrenFor = item.IntersectType;
                if (last) {
                    intersectIdToGetChildrenFor = last.IntersectType;
                }
                return this.fieldsService
                    .getRelationLookupChildIntersectTypes(intersectIdToGetChildrenFor || 0)
                    .then(ci => {
                        item.relationItems = ci;
                    })
                    .then(() => item.relationsLoading = false);
            case ComplexLookupRelationType.ParentItem.toString():
                return this.fieldsService
                    .getParentRelations(object, objectId)
                    .then(pi => {
                        item.relationItems = pi;
                    })
                    .then(() => item.relationsLoading = false);
            case ComplexLookupRelationType.StandardRelationhip.toString():
                return this.fieldsService
                    .getStandardRelations(object, objectId)
                    .then(sr => {
                        item.relationItems = sr;
                    })
                    .then(() => item.relationsLoading = false);
        }
    }

    private changeRel(index: number): Promise<any> {
        let item = this.model.RelationItems[index];
        let last = (index == 0) ? null : this.model.RelationItems[index - 1];
        
        let params = [];
        if (item.selectedRelationItemID) {
            params = item.selectedRelationItemID.split('|');
        } else {
            params.push(item.IntersectType);
            params.push(item.Object);
            params.push(item.ObjectID);
            item.selectedRelationItemID = item.IntersectType + '|' + item.Object + '|' + item.ObjectID;
        }

        try {
            if (params.length < 3)
                return;
            let id = parseInt(params[2]);
            let type = params[1];
            let intersectType = parseInt(params[0]);
            let direction = parseInt(params[3]);

            item.IntersectType = intersectType;
            item.Direction = direction;
            item.Object = type;
            item.ObjectID = id;

            item.DisplayFields = [];
            return this.fieldsService.getRelationLookupDisplayFields(id, type, intersectType)
                .then(r => {
                    r.forEach(i => {
                        let params = i.value.split('|');
                        let d = new FieldTypeItemDisplayFieldEditorModel();
                        d.FieldTypeID = parseInt(params[0]);
                        d.FieldTypeName = params[1];
                        d.Show = false;
                        d.FilterValue = "";
                        d.SortOrder = null;
                        d.value = i.value;
                        let e = item.DisplayFields.find(j => j.FieldTypeID == d.FieldTypeID && j.FieldTypeName == d.FieldTypeName);
                        if (e != null) {
                            e.Show = true;
                            e.value = i.value;
                        } else
                            item.DisplayFields.push(d);
                    });

                    let s = [];
                    for (let i = 1; i <= item.DisplayFields.length; i++) {
                        item.DisplayFields[i - 1].DisplayOrder = i;
                        s.push({ id: i, text: i });
                    }
                    item.SortOrderList = s;

                });

        } catch (e) {
            return Promise.resolve();
        }
    }

    private changeDisplayOrder(item: FieldTypeItemDisplayFieldEditorModel, parent: FieldTypeRelationItemEditorModel) {
        let other = parent.DisplayFields.find(f => f.DisplayOrder == item.DisplayOrder && f.value != item.value);
        if (other) {
            let sum = (parent.DisplayFields.length * (parent.DisplayFields.length + 1)) / 2;
            let total = _.sumBy(parent.DisplayFields, i => { return (i == other) ? 0 : (+i.DisplayOrder || 0); });
            other.DisplayOrder = sum - total;
        }
            
    }

    private changeLegacyRef(): Promise<any> {

        this.childIntersectDisabled = (this.model.RelationItem.ReferenceType.toString() || '1') == '1';
        this.model.RelationItem.DisplayFields = [];
        if (this.model.RelationItem.selectedRelationItemID != null) {
            let params = this.model.RelationItem.selectedRelationItemID.split('|');

            this.model.RelationItem.IntersectType = parseInt(params[0]);
            this.model.RelationItem.Object = params[1];
            this.model.RelationItem.ObjectID = parseInt(params[2]);
            this.model.RelationItem.Direction = parseInt(params[3]);
        }

        if (this.model.RelationItem.IntersectType != null && !this.childIntersectDisabled) {
            this.childIntersectsLoading = true;
            return this.fieldsService.getRelationLookupChildIntersectTypes(this.model.RelationItem.IntersectType)
                .then(r => {
                    this.childIntersectTypes = r;
                    this.childIntersectsLoading = false;
                });
        } else if (this.childIntersectDisabled) {
            return this.changeLegacyChild();
        } else return Promise.resolve();
    }

    private changeLegacyChild(): Promise<any> {

        let intersectType = this.model.RelationItem.IntersectType;
        let type = this.model.RelationItem.Object;
        let id = this.model.RelationItem.ObjectID;

        if (this.model.RelationItem.ReferenceType.toString() != '1') { //not self ref 
            let params = this.model.RelationItem.selectedChildIntersectType.split('|');
            intersectType = parseInt(params[0]);
            type = params[1];
            id = parseInt(params[2]);
        }

        if (intersectType && id && type) {
            let item = this.model.RelationItem;
            item.DisplayFields = [];
            return this.fieldsService.getRelationLookupDisplayFields(id, type, intersectType)
                .then(r => {
                    r.forEach(i => {
                        let params = i.value.split('|');
                        let d = new FieldTypeItemDisplayFieldEditorModel();
                        d.FieldTypeID = parseInt(params[0]);
                        d.FieldTypeName = params[1];
                        d.Show = false;
                        d.FilterValue = "";
                        d.SortOrder = null;
                        d.value = i.value;
                        let e = item.DisplayFields.find(j => j.FieldTypeID == d.FieldTypeID && j.FieldTypeName == d.FieldTypeName);
                        if (e != null) {                            
                            e.Show = true;
                            e.value = i.value;
                        } else
                            item.DisplayFields.push(d);
                    });

                    let s = [];
                    for (let i = 1; i <= item.DisplayFields.length; i++) {
                        item.DisplayFields[i - 1].DisplayOrder = i;
                        s.push({ id: i, text: i });
                    }
                    item.SortOrderList = s;
                });
        } else return Promise.resolve();
    }

    private changeFilteredLookup(): Promise<any> {
        if (this.filteredLookup == null || this.filteredLookup == '') {
            this.filteredLookupDisplayFields = [];
            return Promise.resolve();
        }
        let params = this.filteredLookup.split('|');
        let id = parseInt(params[1]);
        let type = params[0];


        return this.fieldsService.getFilteredLookupDisplayFields(this.objectType, this.objectID, type, id)
            .then(d => {
                this.filteredLookupDisplayFields = d;

                this.filteredSortOrderList = [];
                for (let i = 0; i < this.filteredLookupDisplayFields.length; i++) {
                    this.filteredSortOrderList.push({
                        id: i + 1,
                        text: i + 1
                    });
                }                
            });
    }

    //#endregion

    private selectDisplayToken(value: string) {
        if (value == null || value == '' || value == 'null')
            return;
        if (this.model.FieldType.LookupDisplayFormat == null) {
            this.model.FieldType.LookupDisplayFormat = '';
        }
        this.selectedLookupToken = null;
        this.model.FieldType.LookupDisplayFormat += value;
    }

    private selectEditToken(value: string) {
        if (value == null || value == '' || value == 'null')
            return;
        if (this.model.FieldType.LookupEditFormat == null) {
            this.model.FieldType.LookupEditFormat = '';
        }
        this.selectedFormatToken = null;
        this.model.FieldType.LookupEditFormat += value;
    }

    private validatePattern() {
        if (this.model.FieldType.Pattern > "" && this.testPattern > "") {
            var patternRegex = new RegExp(this.model.FieldType.Pattern);
            this.testPatternValidationText = (patternRegex.test(this.testPattern)) ? 'Success' : 'Fail';
        }
        else {
            this.testPatternValidationText = '';
        }
    }

    private validateNumber(value: string) {
        if (value == 'Number' || value == 'Decimal') {
            if (this.model.FieldType.MinimumLength && this.model.FieldType.MinimumLength > Number.MAX_SAFE_INTEGER) {
                this.errorMessage = 'Please enter a smaller Minimum Value';
                return;
            } else if (this.model.FieldType.MinimumLength && this.model.FieldType.MinimumLength < Number.MIN_SAFE_INTEGER) {
                this.errorMessage = 'Please enter a larger Minimum Value';
                return;
            } else if (this.model.FieldType.MaximumLength && this.model.FieldType.MaximumLength > Number.MAX_SAFE_INTEGER) {
                this.errorMessage = 'Please enter a smaller Maximum Value';
                return;
            } else if (this.model.FieldType.MaximumLength && this.model.FieldType.MaximumLength < Number.MIN_SAFE_INTEGER) {
                this.errorMessage = 'Please enter a larger Maximum Value';
                return;
            } else {
                this.errorMessage = '';
            }

            if (value == 'Number') {
                if (this.model.FieldType.Increment && this.model.FieldType.Increment % 1 != 0) {
                    this.errorMessage = 'Please enter a valid integer.';
                    return;
                } else if (this.model.FieldType.MinimumLength && this.model.FieldType.MinimumLength % 1 != 0) {
                    this.errorMessage = 'Please enter a valid integer.';
                    return;
                } else if (this.model.FieldType.MaximumLength && this.model.FieldType.MaximumLength % 1 != 0) {
                    this.errorMessage = 'Please enter a valid integer.';
                    return;
                } else {
                    this.errorMessage = '';
                }
            }


            if (value == 'Decimal') {
                if (this.model.FieldType.Precision
                    && this.model.FieldType.Precision < 0 || this.model.FieldType.Precision > 38) {
                    this.errorMessage = 'please enter decimal places between 0 and 38.';
                    return;
                }
            }

            if (this.model.FieldType.Increment < 0) {
                this.errorMessage = 'Please enter a positive number for the increment.';
                return;
            } else if (this.model.FieldType.Increment > Number.MAX_SAFE_INTEGER) {
                this.errorMessage = 'Please enter a smaller number for the increment.';
                return;
            } else {
                this.errorMessage = '';
            }

            if (FormHelpers.isNumber(this.model.FieldType.DefaultValue)) {
                if (FormHelpers.isNumber(this.model.FieldType.MinimumLength) && +this.model.FieldType.DefaultValue < this.model.FieldType.MinimumLength) {
                    this.errorMessage = 'Please enter a minimum value of ' + this.model.FieldType.MinimumLength + '.';
                    return;
                }
                else if (FormHelpers.isNumber(this.model.FieldType.MaximumLength) && +this.model.FieldType.DefaultValue > this.model.FieldType.MaximumLength) {
                    this.errorMessage = 'Please enter a maximum value of ' + this.model.FieldType.MaximumLength + '.';
                    return;
                }
                else { 
                    this.errorMessage = '';
                }
            }

            if (FormHelpers.isNumber(this.model.FieldType.MinimumLength) && FormHelpers.isNumber(this.model.FieldType.MaximumLength))
                if (this.model.FieldType.MinimumLength > this.model.FieldType.MaximumLength)
                    this.errorMessage = 'Please enter a minimum value which is lower than the maximum value.';
                else
                    this.errorMessage = '';
        } else {
            this.errorMessage = '';
        }
    }
    
    private CheckMinRequired(fem: FieldTypeEditorModel) {
        if (!fem) {
            return;
        }
        if (fem.FieldType.Type == 'Number' || fem.FieldType.Type == 'Decimal') {
            return false;
        } else {
            return !fem.FieldType.IsRequired;
        }
    }

    private updateApiName(event) {
        this.model.FieldType.Name = event.target.value.replace(/[^a-zA-Z0-9_]/g, ''); 
    }

    private addFusion() {
        let i = new FieldTypeFusionItemEditorModel();
        i.ReferenceType = this.lookups.ReferenceTypes[0].value;
        if (this.model.FusionItems == null) {
            this.model.FusionItems = [];
        }
        this.model.FusionItems.push(i);
    }

    private removeFusion(i: number) {
        this.model.FusionItems.splice(i, 1);
    }

    private addRelation(item: FieldTypeRelationItemEditorModel) {
        let i = new FieldTypeRelationItemEditorModel();
        let params = item.selectedRelationItemID.split('|');
        let id = parseInt(params[2]);
        let type = params[1];
        let intersectType = parseInt(params[0]);

        i.ObjectID = id;
        i.Object = type;
        i.IntersectTypeID = intersectType;
        i.IntersectType = intersectType;
        i.displayValue = item.relationItems.find(i => i.value == item.selectedRelationItemID).title;

        this.model.RelationItems.push(i);
        this.relationItemCount = this.model.RelationItems.length;
    }

    private removeRelation(item: FieldTypeRelationItemEditorModel) {
        //only last item can be deleted
        this.model.RelationItems.pop();
        this.relationItemCount = this.model.RelationItems.length;
    }

    private anyDisplayFieldsSelected(e: any) {
        if (this.model.FieldType.Type != 'ComplexRelationLookup') {
            this.displayFieldSelected = true;
            if (this.lookups.Field_FieldFromRelRelationships.length > 0)
                this.cardinalFieldFromRelationshipSelected(parseInt(this.lookups.Field_FieldFromRelRelationships[0].value));
            return;
        }  
        if (e == true) {
            this.displayFieldSelected = true;
            return;
        }
        this.displayFieldSelected = false;
        this.model.RelationItems.forEach(r => {
            r.DisplayFields.forEach(d => {
                if (d.Show) {
                    this.displayFieldSelected = true;
                    return;
                }
            });
        });
    }
    private onDateSelectMethod(e: Date)
    {
        this.model.FieldType.DefaultValue = e.toISOString();
        this.defaultDate = e;
    }
  
    public isRelationshipWithMultipleCardinality(): boolean {
        
        return true;
    }
}