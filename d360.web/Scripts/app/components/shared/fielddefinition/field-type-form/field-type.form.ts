import * as _ from 'lodash';
import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { SelectItem } from 'primeng/api';

import {
    FieldTypeEditorModel,
    Lookups,
    FieldTypeRelationItemEditorModel,
    FieldTypeItemDisplayFieldEditorModel,
    Direction
} from '../../../../models/fields.model';

import { FieldsObservableService } from '../../../../services/fieldsObservable.service';
import { ObjectDetailService } from '../../../../services/object-detail.service';
import { BaseComponent } from '../../../shared/base.component';
import { FormHelpers } from '../../../../static/form-helpers';
import { Observable, Subscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { MessagesObservableService } from '../../../../services/messages-observable.service';
import { FieldTypeAPIModelField, FieldType, FieldTypeAPIModel, DefinitionField, Relation } from '../../../../models/fieldtype-api.model';
import { AssetService } from '../../../../services/asset.service';
import { CompanySettingsService } from '../../../../services/settings.service';


@Component({
    selector: 'd3s-field-type-form',
    templateUrl: './field-type.form.html',
    styles: [
        `
            .display-table tr td {
                padding: 3px;
                border-radius: 0;
            }

            .relation-table tr td {
                border-radius: 0;
            }

            .display-table-title {
                text-align: center;
                width: 100%;                
                text-transform: uppercase;
                color: #5c5e60 !important;
                font-size: 1rem;
                font-weight: bold;
            }
        
            .sort-container {
                display: flex;
                gap: 12px;
            }
        `
    ],
    providers: [FieldsObservableService, ObjectDetailService, AssetService],
})

export class FieldTypeForm extends BaseComponent implements OnInit, OnChanges {
    @Input() name: string;
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() actionName: string = "Add";
    @Input() objectName: string = '';


    @Output() onComplete = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    @Input() showIsListable: boolean = true;
    @Input() showIsPartOfKey: boolean = true;
    @Input() showShowInDetailTile: boolean = true;
    @Input() showIsEditable: boolean = true;
    @Input() showIsRequired: boolean = true;
    @Input() showDescription: boolean = true;
    @Input() showPersistInFilters: boolean = true;
    @Input() enableAllowMultipleValues: boolean = true;
    @Input() showAddToSearch: boolean = false;

    @Input() showDisplayInColumn: boolean = false;
    @Input() hasDisplayInColumn: boolean = false;

    @Input() actionTypeUid: string;
    @Input() assetTypeUid: string;
    @Input() relationshipTypeUid: string;
    @Input() supportsPrimaryFilterOption: boolean = false;

    @Input() fields: FieldTypeAPIModelField[] = [];

    private currentType: string = "Empty";

    private lookups: Lookups = new Lookups();
    private lookupDefaultValueOptions: SelectItem[];
    private booleanDefaultValueOptions: SelectItem[];
    private scoreTypeOptions: SelectItem[];
    private model: FieldTypeEditorModel;
    private initialItem: FieldTypeEditorModel;
    private isListable: boolean;

    private testPattern: string;
    private testPatternValidationText: string;
    private syncApiNameWithName: boolean = true;

    private relationItemCount = 0;

    private childIntersectTypes: any[] = [];
    private childIntersectsLoading = false;
    private childIntersectDisabled = true;

    private selectedLookupToken = null;
    private selectedFormatToken = null;
    private fieldsFromRelation: SelectItem[] = [];

    private listFilterable: boolean = false;
    private listFilterOptions = new Map();
    private listFilterPredicate: string = null;
    private listFilterPredicates: any[] = [];
    private listFilterRelatedFields: any[] = [];
    private expandFilterConfiguration: boolean = false;

    private displayFieldSelected: boolean = true;
    public listParentFields: SelectItem[] = [];

    public TypeaheadJsonPropertyOptionsForJsonFieldResults: string[] = [];

    private validationErrors: Map<string, string> = new Map<string, string>();
    private errorMessage: string = "";
    private isListableRelationship: boolean = false;

    public defaultDate: any;
    public defaultLinkName: any;
    public defaultLinkAdress: any;

    private minLengthLowerText = 0;
    private minLengthUpperText = 999999;

    private maxLengthLowerText = 0;
    private maxLengthUpperText = 1000000;

    private minLengthLowerNumeric = -9999999999;
    private minLengthUpperNumeric = 9999999999;

    private maxLengthLowerNumeric = -9999999999;
    private maxLengthUpperNumeric = 9999999999;

    numberOfAssetsForType: number = 0;

    private disableFieldTypeSelection: boolean = false;
    public enableListSingleResponsibilityType: boolean = false;

    constructor(private fieldsService: FieldsObservableService,
        private messagesService: MessagesObservableService,
        private objectDetailService: ObjectDetailService,
        private assetService: AssetService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.model = new FieldTypeEditorModel();
        this.model.FieldType = new FieldTypeAPIModelField();
        this.booleanDefaultValueOptions = [
            { label: '-No Default-', value: null },
            { label: 'True', value: true },
            { label: 'False', value: false },
        ]
    }

    ngOnInit() {
        this.initialItem = _.cloneDeep(this.model);

        this.assetService.getAssetCountsByAssetTypeUid(this.assetTypeUid)
            .subscribe((res) => {
                if (res.length > 0) {
                    this.numberOfAssetsForType = +res[0].count + 1;
                }
            });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'name') {
                this.load();
                this.initialItem = _.cloneDeep(this.model);

            }
        }
    }
    currentFieldType(item: FieldTypeAPIModelField): string {
        if (item.Type) {
            return Object.keys(item.Type).filter((key) => { return item.Type[key] !== null })[0];
        }
    }
    //#region load functions

    private getFieldTypeEditorHandler = (responseGetFieldTypeEditor: FieldTypeAPIModelField) => {

        this.currentType = this.currentFieldType(responseGetFieldTypeEditor);
        let DBType = this.currentType;
        this.currentType = this.checkCurrentTypeName(this.currentType);
        if (DBType != this.currentType) {
            //only one type to be defined for editing so remove the missnamed DBType after assigning its values to the correct object
            let correctNameType = new FieldType(this.currentType);
            responseGetFieldTypeEditor.Type[this.currentType] = { ...(correctNameType[this.currentType]), ...responseGetFieldTypeEditor.Type[DBType] };
            responseGetFieldTypeEditor.Type[DBType] = null;
        } else {
            //requires initialising as some parameters like isRequired will be null from the DB
            let intiialisedType = new FieldType(this.currentType);
            responseGetFieldTypeEditor.Type[this.currentType] = { ...(intiialisedType[this.currentType]), ...responseGetFieldTypeEditor.Type[this.currentType] };
        }


        this.model.FieldType = responseGetFieldTypeEditor;
        this.model.cardinalRelationship = null;
        this.model.selectedLookup = null;
    };

    private getLookupsHandler = (responseGetLookups) => {
        this.lookups = responseGetLookups;
        this.lookups.Lookups = this.lookups.Lookups.map(x => {
            if (x.value.length && x.value.length == 36)
                return { value: x.value.toLowerCase(), label: x.label };
            else
                return { value: x.value, label: x.label };
        });
        this.lookups.ReferenceTypes = this.fieldsService.getReferenceTypes();
        this.lookups.Field_JsonDataTypes.unshift({ label: 'Choose..', value: null });
        this.lookups.Field_JsonFields.unshift({ label: 'Choose..', value: null });
        this.lookups.DataTypes.unshift({ label: 'Choose..', value: null });
    };

    private getFormDataHandler = (responseGetFormData) => {
        if (responseGetFormData) {
            this.model.RelationItems = responseGetFormData.RelationItems;

            if (this.model.RelationItems && this.currentType == 'ComplexRelationLookup') {
                this.loadComplexRelationLookup();
            }
        }
    };

    private load(): void {
        if (this.name && (this.assetTypeUid || this.actionTypeUid || this.relationshipTypeUid)) {
            this.actionName = 'Edit';
            this.isLoading = true;

            this.fieldsService.getFieldTypeEditor(this.name, this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid)
                .subscribe(ret => {
                    this.getFieldTypeEditorHandler(ret);
                    this.fieldsService.getLookups(this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid)
                        .subscribe(s => {
                            this.getLookupsHandler(s);
                            this.fieldsService.getFormData(this.name, this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid)
                                .subscribe(formData => {
                                    this.getFormDataHandler(formData);
                                    this.loadDataType(this.currentFieldType(this.model.FieldType), true);

                                    var lockedNames: string[] = ['Name', 'GovernanceRole', 'StepNo'];
                                    if (this.objectType == 'TaskType' && lockedNames.some(x => x == this.name)) {
                                        this.disableFieldTypeSelection = true;
                                    }

                                    this.isLoading = false;
                                });
                        });
                });
        } else if (this.assetTypeUid || this.actionTypeUid || this.relationshipTypeUid) {
            this.actionName = 'Add';
            this.isLoading = true;
            this.model = new FieldTypeEditorModel();
            this.model.FieldType = new FieldTypeAPIModelField();

            this.fieldsService.getLookups(this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid)
                .subscribe(x => {
                    this.getLookupsHandler(x);
                    this.model.FieldType.Type = new FieldType(); //Set as Empty to allow for selection.
                    this.isLoading = false;
                });
        }
    }

    private loadComplexRelationLookup() {
        //load existing values
        this.model.RelationItems.forEach(r => {

            r.DisplayFields.forEach(
                d => {
                    if (d.FieldTypeID == null && d.value) {
                        d.FieldTypeID = parseInt(d.value.split('|')[0]);
                    }

                    if (d.FieldTypeName == null && d.value) {
                        d.FieldTypeName = d.value.split('|')[1];
                    }

                    if (!d.value) {
                        d.value = d.FieldTypeID + '|' + d.FieldTypeName;
                    }
                }
            );

        });


        let clone = _.cloneDeep(this.model.RelationItems);
        if (this.model.RelationItems != null && this.model.RelationItems.length) {
            for (let i = 0; i < this.model.RelationItems.length; i++) {
                let item = this.model.RelationItems[i];

                //load cascading dropdowns
                this.loadRelationItems(i).subscribe(
                    () => {
                        item.selectedRelationItemID = item.IntersectTypeUid.toUpperCase() + '|' + item.AssetTypeUid.toUpperCase() + '|' + item.Direction;
                        this.changeRel(i).subscribe(() => {
                            let parent = item;
                            item.DisplayFields.forEach(
                                d => {
                                    let item = clone[i].DisplayFields.find(f => f.FieldTypeID == d.FieldTypeID && f.FieldTypeName == d.FieldTypeName);

                                    if (item) {
                                        d.Show = item.Show;
                                        d.DisplayOrder = item.DisplayOrder;
                                        d.Filter = item.Filter;
                                        d.OverrideDisplayName = item.OverrideDisplayName;
                                        d.SortOrder = item.SortOrder;
                                        d.Width = item.Width;

                                        if (d.DisplayOrder != null) {
                                            this.changeDisplayOrder(item, parent);
                                        }
                                    }

                                }
                            );

                            let r = item.relationItems.find(f => f.value == item.selectedRelationItemID);
                            if (i > 0) {
                                r = this.model.RelationItems[i - 1].relationItems.find(f => f.value == this.model.RelationItems[i - 1].selectedRelationItemID);
                            }

                            if (r) {
                                item.displayValue = r.title;
                            }
                        });
                    }
                );

                //load display order/sort order drop down lists
                this.model.RelationItems.forEach(
                    r => {
                        let s = [];

                        for (let i = 1; i <= r.DisplayFields.length; i++) {
                            r.DisplayFields[i - 1].DisplayOrder = i;
                            s.push({ id: i, text: i });
                        }

                        r.SortOrderList = s;
                    }
                );

                this.relationItemCount = this.model.RelationItems.length;
            }
        }
    }

    private loadDataType(value: string, isFromLoad: boolean = false) {
        let observables: Array<Observable<any>> = [];
        this.showDescription = true;
        this.enableAllowMultipleValues = true;
        this.hasDisplayInColumn = true;
        this.showIsRequired = true;

        if (value == null) {
            this.currentType = "Empty";
            this.model.FieldType.Type = new FieldType("Empty");
            return;
        }

        if (!isFromLoad)
            this.model.FieldType.Type = new FieldType(value);

        switch (value.toLowerCase()) {
            case 'lookup':
                if (this.model.FieldType.Type[this.currentType].List && this.model.FieldType.Type[this.currentType].List.Uid) {
                    observables.push(this.lookupTypeSelected(this.model.FieldType.Type[this.currentType].List.Uid));
                    this.model.FieldType.Type['Lookup'].AllowMultipleValues = this.model.FieldType.Type['Lookup'].List.AllowMultipleValues;
                }
                else if (this.model.FieldType.Type[this.currentType].List && this.model.FieldType.Type['Lookup'].List.Class && !this.model.FieldType.Type[this.currentType].List.Uid) {
                    let valToPass = this.model.FieldType.Type['Lookup'].List.Class == 'Reference' ? 'ReferenceItemType' : 'TaxonomyType';
                    this.model.FieldType.Type['Lookup'].AllowMultipleValues = this.model.FieldType.Type['Lookup'].List.AllowMultipleValues;
                    observables.push(this.lookupTypeSelected(valToPass));
                }
                else {
                    this.model.FieldType.Type[this.currentType].List.Uid = this.lookups.Lookups[0].value;
                    observables.push(this.lookupTypeSelected(this.lookups.Lookups[0].value));
                    this.model.FieldType.Type['Lookup'].AllowMultipleValues = this.model.FieldType.Type['Lookup'].List.AllowMultipleValues;
                }
                break;
            case 'relationship':
                try {
                    if (this.model.FieldType.Type["Relationship"].IntersectTypeUid) {
                        observables.push(this.cardinalRelationshipSelected(this.model.FieldType.Type["Relationship"].IntersectTypeUid));
                    }
                    if (!this.model.FieldType.Type["Relationship"].IsEditable) {
                        this.showDescription = false;
                        this.model.FieldType.Type["Relationship"].Description.Form = "";
                    }
                } catch (e) {
                    console.log(e);
                }
                break;
            case 'fieldfromrelationship':
            case 'computedrelationshipfield':
                try {
                    if (this.model.FieldType.Type["FieldFromRelationship"].IntersectTypeUid) {
                        observables.push(this.cardinalFieldFromRelationshipSelected(this.model.FieldType.Type["FieldFromRelationship"].IntersectTypeUid, this.model.FieldType.Type["FieldFromRelationship"].FieldTypeName));
                    } else if (this.lookups.Field_CardinalRelationships.length > 0) {
                        observables.push(this.cardinalFieldFromRelationshipSelected(this.lookups.Field_FieldFromRelRelationships[0].value,
                            this.model.FieldType.Type["FieldFromRelationship"].FieldTypeName));
                    }
                    this.model.FieldType.Type.FieldFromRelationship.IsEditable = false;
                    this.showDescription = false;
                } catch (e) {
                    console.log(e);
                }
                break;
            case 'reflistrelationship':
                this.hasDisplayInColumn = false;
                try {
                    if (this.model.cardinalRelationship && (this.lookups.Field_CardinalReferenceRelationships.length > 0)
                        && (this.lookups.Field_CardinalReferenceRelationships.find(x => x.value == this.model.cardinalRelationship))) {
                        observables.push(this.cardinalFieldFromRelationshipSelected(this.model.cardinalRelationship));
                    } else if (this.lookups.Field_CardinalReferenceRelationships.length > 0) {
                        observables.push(this.cardinalFieldFromRelationshipSelected(this.lookups.Field_CardinalReferenceRelationships[0].value));
                    }
                    this.showDescription = false;
                } catch (e) {
                    console.log(e);
                }
                break;
            case 'complexrelationlookup':
                this.showDescription = false;
                this.hasDisplayInColumn = false;
                if (this.model.RelationItems == null || this.model.RelationItems.length == 0) {
                    let r = new FieldTypeRelationItemEditorModel();

                    r.DisplayFields = [];
                    r.AssetTypeUid = this.GetCurrentUid()

                    this.model.RelationItems = [];
                    this.model.RelationItems.push(r);
                    this.relationItemCount = 1;
                    this.loadRelationItems(this.model.RelationItems.length - 1).subscribe();
                }
                break;
            case 'tag':
                if (!isFromLoad)
                    this.showIsEditable = false;
                this.showDescription = false;
                this.enableAllowMultipleValues = false;
                this.hasDisplayInColumn = false;
                break;
            case "ownershiplookup":
                this.showDescription = false;
                this.onEnableListSingleResponsibilityType(this.model.FieldType.Type[this.currentType].Definition.ResponsibilityTypeUid?.length > 1)
                break;
            case 'computedownershiplookup':
            case 'json':
            case 'jsonelement':
                this.hasDisplayInColumn = false;
            case 'path':
                this.showDescription = false;
                break;
            case 'score':
                observables.push(this.loadAvailableScoreTypes());
                this.enableAllowMultipleValues = false;
                this.showDescription = false;
                break;
            case 'counter':
                this.model.FieldType.Type.Counter.ShowIfEmpty = true;
                if (!this.model.FieldType.Type.Counter.CounterInitialIndex) {
                    this.model.FieldType.Type.Counter.CounterInitialIndex = this.numberOfAssetsForType;
                }
                this.showIsRequired = false;
                this.enableAllowMultipleValues = false;
                this.showDescription = false;
                break;
            default:
                break;
        }
        if (this.currentType == 'Date' && this.model.FieldType.Type[this.currentType].DefaultValue != undefined) {
            this.defaultDate = new Date(this.model.FieldType.Type[this.currentType].DefaultValue);
        }

        if (this.currentType == 'Link' && this.model.FieldType.Type[this.currentType].DefaultValue != null) {
            this.defaultLinkName = this.model.FieldType.Type[this.currentType].DefaultValue.Text;
            this.defaultLinkAdress = this.model.FieldType.Type[this.currentType].DefaultValue.Url;
        }

        this.errorMessage = ""; //clear the error message when changing types

        observables
            .filter(x => x != null && x != undefined)
            .forEach(obs => obs.pipe(map(() => this.validate('*'))).subscribe());
    }

    // called when the lookup type field is changed
    private lookupTypeSelected(uid: string, cleardisplays: boolean = false): Observable<any> {
        if (uid == undefined) {
            console.log("[ERROR] - LOOKUP TYPE UID IS UNDEFINED", uid);
            return null;
        }
        if (this.currentType == 'Lookup') {
            if (cleardisplays) {
                this.model.FieldType.Type[this.currentType].Format.Display = "";
                this.model.FieldType.Type[this.currentType].Format.Edit = "";
            }
            if (!this.isUid(uid)) {
                this.model.FieldType.Type[this.currentType].List.Uid = uid;
                this.model.FieldType.Type[this.currentType].List.Class = uid;
            }

            this.loadDefaultValueOptions(uid);
            this.loadHierarchyOptions(uid);
            this.loadListFilterOptions(uid);

            this.validate('*');

            return this.loadTokens(uid);
        }
    }
    private isUid(value: string) {
        let regex = /[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}/;
        return regex.test(value);
    }
    // called when the lookup type field is changed
    private cardinalRelationshipSelected(value: string): Observable<any> {
        if (value == undefined) {
            console.log("[ERROR] - Intersect TYPE IS UNDEFINED", value);
            return Observable.create();
        }
        this.isListableRelationship = false;

        //update the model to have correct lookuptype object and id
        this.model.FieldType.Type["Relationship"].IntersectTypeUid = value.toLocaleLowerCase();

        return this.fieldsService.getRelationshipFieldIsListable(value, this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid)
            .pipe(map(res => {
                this.isListableRelationship = res;
                if (!this.isListableRelationship)
                    this.model.FieldType.Type[this.currentType].IsListable = this.isListableRelationship;
            }));
    }

    private cardinalFieldFromRelationshipSelected(value: string, fieldTypename: string = null): Observable<any> {

        if (value == undefined) {
            console.log("[ERROR] - Intersect TYPE IS UNDEFINED", value);
            return Observable.create();
        }

        return this.fieldsService.getRelationObjectFields(this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid, value)
            .pipe(map(
                d => {
                    this.fieldsFromRelation = d;
                    if (fieldTypename != null) {
                        this.model.FieldType.Type[this.currentType].FieldTypeName = fieldTypename;
                    } else if (this.fieldsFromRelation.length > 0) {
                        this.model.FieldType.Type[this.currentType].FieldTypeName = this.fieldsFromRelation[0].label;
                    } else {
                        this.model.FieldType.Type[this.currentType].FieldTypeName = null;
                    }

                    if (!this.model.FieldType.Type[this.currentType].IntersectTypeUid) {
                        if (this.currentType == 'RefListRelationship') {
                            if (this.lookups.Field_CardinalReferenceRelationships
                                && this.lookups.Field_CardinalReferenceRelationships.length > 0) {
                                this.model.FieldType.Type[this.currentType].IntersectTypeUid = this.lookups.Field_CardinalReferenceRelationships[0].value;
                            }
                        }

                        if (this.currentType == 'FieldFromRelationship') {
                            if (this.lookups.Field_FieldFromRelRelationships
                                && this.lookups.Field_FieldFromRelRelationships.length > 0) {
                                this.model.FieldType.Type[this.currentType].IntersectTypeUid = this.lookups.Field_FieldFromRelRelationships[0].value;
                            }
                        }
                    }
                }
            ));
    }

    private cardinalReferenceItemListFromRelationshipSelected(value: string) {
        if (value == undefined) {
            console.log("[ERROR] - Intersect TYPE IS UNDEFINED", value);
        }
        //update the model to have correct lookuptype object and id
        this.model.FieldType.Type[this.currentType].IntersectTypeUid = value.toLocaleLowerCase();
    }

    private cardinalFieldFromRelationship_FieldSelected(value: string) {
        this.model.FieldType.Type[this.currentType].FieldTypeName = value;
    }

    private loadHierarchyOptions(uid: string): void {
        this.listParentFields = [];


        this.fieldsService.getReferenceTypeHierarchyFields(uid, this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid).subscribe(
            r => {
                this.listParentFields = r.map((x) => { return { label: x.label, value: x.value }; });

                if (this.listParentFields == null || this.listParentFields.length == 0) {
                    this.model.FieldType.Type[this.currentType].ParentFieldTypeName = null;
                }
            }
        );
    }

    private loadListFilterOptions(uid: string): void {
        this.listFilterable = false;
        this.listFilterPredicates = [];
        this.listFilterRelatedFields = [];
        this.listFilterOptions.clear();

        this.fieldsService.getListFilterOptions(uid, this.assetTypeUid, this.actionTypeUid, this.relationshipTypeUid).subscribe(
            r => {
                this.listFilterable = true;
                r.forEach(
                    d => {
                        if (!this.listFilterOptions.has(d.PredicateValue)) {
                            this.listFilterOptions.set(d.PredicateValue, {
                                value: d.PredicateValue,
                                label: d.PredicateName,
                                fieldtypeOptions: (this.objectType == 'IssueType') ? [{
                                    value: null,
                                    label: "Action Subject",
                                    info: "Model/Artifact"
                                }] : []
                            });
                        }

                        if (d.FieldTypeName != null && d.Name != this.name) {
                            this.listFilterOptions.get(d.PredicateValue).fieldtypeOptions.push({
                                value: d.FieldTypeName,
                                label: d.FriendlyName,
                                info: d.Info
                            });
                        }
                    }
                );

                this.listFilterPredicates.push({ value: null, label: 'Choose..' });
                this.listFilterOptions.forEach(
                    d => {
                        if (d.fieldtypeOptions.length > 0)
                            //only include predicates with possible field options
                            this.listFilterPredicates.push({ value: d.value, label: d.label });
                    }
                );

                if (this.listFilterPredicates.length == 1) {
                    //If we have no predicates to select, turn off filter configuration
                    this.listFilterable = false;
                    this.selectPredicate(null);
                    this.expandFilterConfiguration = false;
                    return;
                }

                if (this.model.FieldType.Type["Lookup"].Filter.PredicateUid != null && this.model.FieldType.Type["Lookup"].Filter.UseDirection != null) {
                    this.selectPredicate(this.model.FieldType.Type["Lookup"].Filter.PredicateUid + '|' + (this.model.FieldType.Type["Lookup"].Filter.UseDirection ? '1' : '0'));
                    this.expandFilterConfiguration = true;
                } else {
                    this.selectPredicate(null);
                    this.expandFilterConfiguration = false;
                }
            }
        );
    }

    private selectPredicate(value: string) {
        if (this.listFilterOptions.has(value)) {
            this.listFilterRelatedFields = this.listFilterOptions.get(value).fieldtypeOptions;
            if (this.model.FieldType.Type["Lookup"].Filter.FieldTypeName == null && this.listFilterRelatedFields.length > 0) {
                this.model.FieldType.Type["Lookup"].Filter.FieldTypeName = this.listFilterRelatedFields[0].value;
            }
        } else {
            value = null;
            this.listFilterRelatedFields = [];
            this.model.FieldType.Type["Lookup"].Filter.FieldTypeName = null;
        }

        if (value == null || value == '' || value == 'null') {
            this.model.FieldType.Type["Lookup"].Filter.PredicateUid = null;
            this.model.FieldType.Type["Lookup"].Filter.UseDirection = null;
        } else {
            this.model.FieldType.Type["Lookup"].Filter.PredicateUid = value.split('|')[0];
            this.model.FieldType.Type["Lookup"].Filter.UseDirection = parseInt(value.split('|')[1]) == 1;
        }

        this.listFilterPredicate = value;
        return;
    }

    private loadDefaultValueOptions(uid: string): Subscription {
        if (uid == undefined) {
            console.log("[ERROR] - NO UID SPECIFIED TO LOAD DEFAULT VALUES FOR ", this.model.FieldType.Type[this.currentType].List.Uid);
            return;
        }


        return this.fieldsService.getLookupDefaultValueOptions(uid).pipe(
            map(r => {
                this.lookupDefaultValueOptions = r;
                if (this.model.FieldType.Type[this.currentType].DefaultValue) {
                    var item = this.lookupDefaultValueOptions.filter((x) => {
                        if (!x.value)
                            return false;
                        return x.value.toString().toLowerCase() == this.model.FieldType.Type[this.currentType].DefaultValue.toString().toLowerCase();
                    })[0];
                    if (item)
                        this.model.FieldType.Type[this.currentType].DefaultValue = item.value;
                }
            })
        ).subscribe();
    }

    private loadTokens(uid: string): Observable<any> {
        if (uid == undefined) {
            console.log("[ERROR] - NO Uid SPECIFIED TO LOAD TOKENS FOR ", this.model.FieldType.Type[this.currentType].List.Uid);
            return;
        }


        return this.fieldsService.getLookupTokens(uid).pipe(map(
            r => {
                this.model.LookupTokens = r;
                if (this.model.LookupTokens && this.model.LookupTokens.length > 0) {
                    if (
                        (this.model.FieldType.Type[this.currentType].Format.Display == null
                            || this.model.FieldType.Type[this.currentType].Format.Display.length == 0)
                    ) {
                        this.model.FieldType.Type[this.currentType].Format.Display = this.model.LookupTokens[0].value;
                    }

                    if (
                        (this.model.FieldType.Type[this.currentType].Format.Edit == null
                            || this.model.FieldType.Type[this.currentType].Format.Edit.length == 0)
                    ) {
                        this.model.FieldType.Type[this.currentType].Format.Edit = this.model.LookupTokens[0].value;
                    }
                }

            }
        ));
    }



    private loadAvailableScoreTypes(): Observable<any> {
        return this.fieldsService.getAvailableScoreTypes(this.assetTypeUid)
            .pipe(
                map(r => {
                    this.scoreTypeOptions = r;
                    this.scoreTypeOptions.unshift({ label: 'Choose..', value: null });

                })
            );
    }

    //#endregion

    //#region form actions

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private onSubmit(): any {
        //convert DisplayFields to objects
        this.isLoading = true;

        if (this.currentType == 'Link') {
            {
                if (!this.defaultLinkName && !this.defaultLinkAdress)
                    this.model.FieldType.Type[this.currentType].DefaultValue = null;
                else {
                    this.model.FieldType.Type[this.currentType].DefaultValue.Text = this.defaultLinkName;
                    this.model.FieldType.Type[this.currentType].DefaultValue.Url = this.defaultLinkAdress;
                }
            }
        } else if (this.currentType == 'Date') {
            this.model.FieldType.Type[this.currentType].DefaultValue = this.defaultDate;
        }


        let apiModel = new FieldTypeAPIModel();
        apiModel.Action = "Merge";
        apiModel.ActionTypeUid = this.actionTypeUid;
        apiModel.AssetTypeUid = this.assetTypeUid;
        apiModel.RelationshipTypeUid = this.relationshipTypeUid;

        //fix the object names so the API can serialise them
        if (this.currentType == 'FieldFromRelationship')
            this.model.FieldType.Type.ComputedRelationshipField = this.model.FieldType.Type.FieldFromRelationship;
        if (this.currentType == "OwnershipLookup")
            this.model.FieldType.Type.ComputedOwnershipLookup = this.model.FieldType.Type.OwnershipLookup;
        if (this.currentType == "RefListRelationship")
            this.model.FieldType.Type.ComputedRelationshipReferenceList = this.model.FieldType.Type.RefListRelationship;
        if (this.currentType == "JSON")
            this.model.FieldType.Type.Json = this.model.FieldType.Type.JSON;
        if (this.currentType == "ComplexRelationLookup") {
            //need to convert the Fields and Relationships to the API expected format
            this.ConvertDisplayFieldsToAPIDefinition();
            this.model.FieldType.Type.ComputedRelationshipLookup = this.model.FieldType.Type.ComplexRelationLookup;
            this.model.FieldType.Type.ComplexRelationLookup = undefined;
        }
        //special cases for Model and reference item types 
        if (this.currentType == 'Lookup') {
            if (!this.isUid(this.model.FieldType.Type.Lookup.List.Uid)) {
                this.model.FieldType.Type.Lookup.List.Uid = null;
                if (this.model.FieldType.Type.Lookup.List.Class == 'TaxonomyType')
                    this.model.FieldType.Type.Lookup.List.Class = 'Model';
            }
            this.model.FieldType.Type.Lookup.List.AllowMultipleValues = this.model.FieldType.Type.Lookup.AllowMultipleValues;
        }


        //add the fieldtype to the API model as an array
        apiModel.Fields = [this.model.FieldType];

        this.fieldsService.putFieldsV2(apiModel).subscribe(
            r => {
                if (r && r.Success) {
                    r.Message = this.actionName == "Edit" ? "Field Type successfully updated" : "Field Type successfully added";
                    this.showMessageForApiResponse(this.messagesService, r);
                    this.model.FieldType.Type = new FieldType("Empty");
                    this.onComplete.emit({ action: this.actionName.toLowerCase(), field: this.model });
                }
                this.isLoading = false;
            }
        );
    }

    private valid(): boolean {
        let valid = true;
        if (this.currentType == 'Empty' || this.currentType == null) {
            valid = false;
        }
        if (this.currentType == 'RefListRelationship' && !this.model.FieldType.Type[this.currentType].IntersectTypeUid) {
            valid = false;
        }
        if (this.currentType == 'FieldFromRelationship' && !this.model.FieldType.Type[this.currentType].IntersectTypeUid) {
            valid = false;
        }

        if (this.currentType == 'Relationship' && !this.model.FieldType.Type[this.currentType].IntersectTypeUid) {
            valid = false;
        }

        if (this.currentType == 'Lookup' && !this.model.FieldType.Type[this.currentType].List.Uid) {
            valid = false;
        }

        if (this.currentType == 'Lookup' && this.model.FieldType.Type[this.currentType].AllowAllValue && !this.model.FieldType.Type[this.currentType].AllowAllLabel) {
            valid = false;
        }

        if (this.currentType == 'Score' && !this.model.FieldType.Type[this.currentType].ScoreType) {
            valid = false;
        }

        if (this.currentType == 'JsonElement') {
            if (!this.model.FieldType.Type[this.currentType].JsonAttribute.FieldName
                || !this.model.FieldType.Type[this.currentType].JsonAttribute.Path ||
                !this.model.FieldType.Type[this.currentType].JsonAttribute.DataType)
                valid = false;
        }

        if (this.currentType === "OwnershipLookup") {
            if (this.enableListSingleResponsibilityType) {
                return this.model.FieldType.Type[this.currentType].Definition.ResponsibilityTypeUid !== null;
            }
        }

        if (!this.isValidationPatternValid()) {
            valid = false;
        }

        return valid;
    }

    //#endregion

    //#region dropdown functions

    private loadRelationItems(index: number): Observable<any> {
        let item = this.model.RelationItems[index];
        let last = (index == 0) ? null : this.model.RelationItems[index - 1];
        item.relationsLoading = true;
        item.DisplayFields = [];
        let uid = this.GetCurrentUid();
        if (index !== 0) {
            uid = last.AssetTypeUid;
        }

        return this.fieldsService.getStandardRelations(uid)
            .pipe(map(
                x => {
                    item.relationItems = x;
                }
            ), map(() => item.relationsLoading = false));
        
    }
    private GetCurrentUid() {
        if (this.assetTypeUid != null)
            return this.assetTypeUid;
        else if (this.actionTypeUid != null)
            return this.actionTypeUid;
        else if (this.relationshipTypeUid != null)
            return this.relationshipTypeUid;
    }
    private changeRel(index: number): Observable<any> {
        let item = this.model.RelationItems[index];

        let params = [];
        if (item.selectedRelationItemID) {
            params = item.selectedRelationItemID.split('|');
        } else {
            params.push(item.IntersectTypeUid);
            params.push(item.AssetTypeUid);
            item.selectedRelationItemID = item.IntersectTypeUid + '|' + item.AssetTypeUid;
        }

        try {
            if (params.length < 3) {
                return;
            }

            let intersectType = params[0];
            let assetTypeUid = params[1];
            let direction = params[2];

            item.IntersectTypeUid = intersectType.toLowerCase();
            item.Direction = direction;
            item.AssetTypeUid = assetTypeUid;
            item.DisplayFields = [];
            return this.fieldsService.getRelationLookupDisplayFields(assetTypeUid, intersectType)
                .pipe(map(
                    r => {
                        r.forEach(
                            i => {
                                let params = i.value.split('|');
                                let d = new FieldTypeItemDisplayFieldEditorModel();

                                d.FieldTypeID = parseInt(params[0]);
                                d.FieldTypeName = params[1];
                                d.Show = false;
                                d.Filter = "";
                                d.SortOrder = null;
                                d.value = i.value;

                                let e = item.DisplayFields.find(j => j.FieldTypeID == d.FieldTypeID && j.FieldTypeName == d.FieldTypeName);

                                if (e != null) {
                                    e.Show = true;
                                    e.value = i.value;
                                } else {
                                    item.DisplayFields.push(d);
                                }
                            });

                        let s = [];
                        for (let i = 1; i <= item.DisplayFields.length; i++) {
                            item.DisplayFields[i - 1].DisplayOrder = i;
                            s.push({ id: i, text: i });
                        }

                        item.SortOrderList = s;
                    }
                ));
        } catch (e) {
            return Observable.create();
        }
    }

    private changeDisplayOrder(item: FieldTypeItemDisplayFieldEditorModel, parent: FieldTypeRelationItemEditorModel) {
        let other = parent.DisplayFields.find(f => f.DisplayOrder == item.DisplayOrder && f.value != item.value);

        if (other) {
            let sum = (parent.DisplayFields.length * (parent.DisplayFields.length + 1)) / 2;
            let total = _.sumBy(parent.DisplayFields,
                i => {
                    return (i == other) ? 0 : (+i.DisplayOrder || 0);
                }
            );

            other.DisplayOrder = sum - total;
        }
    }

    //#endregion

    searchJsonForProperty(event) {
        this.fieldsService.getTypeaheadJsonPropertyOptionsForJsonField(
            this.model.FieldType.Type[this.currentType].JsonAttribute.FieldName,
            event.query,
            this.assetTypeUid,
            this.actionTypeUid,
            this.relationshipTypeUid).subscribe(data => {
                this.TypeaheadJsonPropertyOptionsForJsonFieldResults = data;
            });
    }

    private selectDisplayToken(value: string) {
        if (value == null || value == '' || value == 'null') {
            return;
        }

        if (this.model.FieldType.Type[this.currentType].Format.Display == null) {
            this.model.FieldType.Type[this.currentType].Format.Display = '';
        }

        this.selectedLookupToken = null;
        this.model.FieldType.Type[this.currentType].Format.Display += value;
    }

    private selectEditToken(value: string) {
        if (value == null || value == '' || value == 'null') {
            return;
        }

        if (this.model.FieldType.Type[this.currentType].Format.Edit == null) {
            this.model.FieldType.Type[this.currentType].Format.Edit = '';
        }

        this.selectedFormatToken = null;
        this.model.FieldType.Type[this.currentType].Format.Edit += value;
    }

    private validatePattern() {
        if (this.model.FieldType.Type[this.currentType].Validation.Pattern > "" && this.testPattern > "") {
            var patternRegex = new RegExp(this.model.FieldType.Type[this.currentType].Validation.Pattern);
            this.testPatternValidationText = (patternRegex.test(this.testPattern)) ? 'Success' : 'Fail';
        } else {
            this.testPatternValidationText = '';
        }

        this.validate('Pattern');
    }

    private setValidation(validation_identifier: string, message: string, test: boolean) {
        if (test) {
            this.validationErrors.set(validation_identifier, message);
        } else {
            this.validationErrors.delete(validation_identifier);
        }
    }

    private validate(fieldname) {
        if (fieldname == undefined) {
            fieldname = '*';
        }
        if (fieldname == '*') {
            this.validationErrors.clear();
        }

        if (fieldname == '*' || fieldname == "NameTaken") {
            this.setValidation('name_already_taken', 'API Name already in use.', (() => {
                if (this.model.FieldType.Name && this.actionName == 'Add') {
                    if (this.fields && this.fields.length > 0) {
                        return this.fields.filter((x) => {
                            return x.Name.toLowerCase().trim() == this.model.FieldType.Name.toLowerCase().trim();
                        }).length > 0;
                    } else
                        return false;
                } else {
                    return false;
                }

            })());
        }

        if (fieldname == '*' || fieldname == 'NameTaken') {
            this.setValidation('name_already_taken', 'API Name not allowed.', (() => {
                if (this.model.FieldType.Name) {
                    var dissallowedFields: string[] = ['id', 'uid', 'assetid', 'assetuid', 'assettypeid',
                        'assettypeuid', 'createdon', 'updatedon', 'parentdisplayname', 'parentassetuid',
                        'keypath', 'displayvalue'];
                    if (this.objectType === 'IntersectType') {
                        dissallowedFields.push('source');
                    }
                    if (this.objectType === 'ResourceType') {
                        dissallowedFields.push('firstname', 'lastname', 'email', 'status', 'state', 'resourceid', 'resourceuri', 'datelastloggedin', 'lastloggedinon', 'isadministrator');
                    }
                    if (dissallowedFields.some(x => x == this.model.FieldType.Name.toLowerCase().trim())) {
                        return true;
                    }
                    return false;
                }
            })());
        }

        if (this.currentType == 'Number' || this.currentType == 'Decimal') {
            if (fieldname == '*' || fieldname == 'MinimumLength') {
                this.setValidation('MinimumLength_toobig', 'Please enter a smaller Minimum Value.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation && this.model.FieldType.Type[this.currentType].Validation.MinimumValue > this.minLengthUpperNumeric);
                })());
                this.setValidation('MinimumLength_toosmall', 'Please enter a larger Minimum Value.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation && this.model.FieldType.Type[this.currentType].Validation.MinimumValue < this.minLengthLowerNumeric);
                })());
            }

            if (fieldname == '*' || fieldname == 'MaximumLength') {
                this.setValidation('MaximumLength_toobig', 'Please enter a smaller Maximum Value.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation.MaximumValue && this.model.FieldType.Type[this.currentType].Validation.MaximumValue > this.maxLengthUpperNumeric);
                })());
                this.setValidation('MaximumLength_toosmall', 'Please enter a larger Maximum Value.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation.MaximumValue && this.model.FieldType.Type[this.currentType].Validation.MaximumValue < this.maxLengthLowerNumeric);
                })());
            }

            if (fieldname == '*' || fieldname == 'Increment') {
                this.setValidation('Increment_negative', 'Please enter a positive number for the increment.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Increment < 0);
                })());

                this.setValidation('Increment_toobig', 'Please enter a smaller number for the increment.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Increment > Number.MAX_SAFE_INTEGER);
                })());
            }
        }

        if (this.currentType == 'Number') {
            if (fieldname == '*' || fieldname == 'Increment') {
                this.setValidation('Increment_integer', 'Please enter a valid integer for Increment.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Increment && this.model.FieldType.Type[this.currentType].Increment % 1 != 0);
                })());
            }

            if (fieldname == '*' || fieldname == 'MinimumLength') {
                this.setValidation('MinimumLength_integer', 'Please enter a valid integer for Minimum Value.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation && this.model.FieldType.Type[this.currentType].Validation.MinimumValue % 1 != 0);
                })());
            }

            if (fieldname == '*' || fieldname == 'MaximumLength') {
                this.setValidation('MaximumLength_integer', 'Please enter a valid integer for Maximum Value.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation.MaximumValue && this.model.FieldType.Type[this.currentType].Validation.MaximumValue % 1 != 0);
                })());
            }

            if (fieldname == '*' || fieldname == 'DefaultValue') {
                this.setValidation('default_integer', 'Please enter a valid integer for Default Value.', (() => {
                    return (this.model.FieldType.Type[this.currentType].DefaultValue && +this.model.FieldType.Type[this.currentType].DefaultValue % 1 != 0);
                })());
            }
        }

        if (this.currentType == 'Decimal') {
            if (fieldname == '*' || fieldname == 'Precision') {
                this.setValidation('precision_range', 'Please enter decimal places between 0 and 5.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation.Precision && this.model.FieldType.Type[this.currentType].Validation.Precision < 0 || this.model.FieldType.Type[this.currentType].Validation.Precision > 5);
                })());
            }
            if (fieldname == '*' || fieldname == 'Precision' || fieldname == 'DefaultValue') {
                if (this.model.FieldType.Type[this.currentType].Validation.Precision && FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].DefaultValue)) {
                    let asString = '' + this.model.FieldType.Type[this.currentType].DefaultValue;

                    if (asString.split('.').length == 2 && asString.split('.')[1].length >= this.model.FieldType.Type[this.currentType].Validation.Precision) {
                        let val = +this.model.FieldType.Type[this.currentType].DefaultValue;
                        let newVal = +val.toFixed(this.model.FieldType.Type[this.currentType].Validation.Precision);

                        if (newVal != null && (newVal != 0 || newVal != +val) && !isNaN(newVal)) {
                            this.model.FieldType.Type[this.currentType].DefaultValue = newVal;
                        }
                    }
                }
            }
        }

        if (this.currentType == 'Number' || this.currentType == 'Decimal') {
            if (fieldname == '*' || fieldname == 'MinimumLength' || fieldname == 'DefaultValue') {
                this.setValidation('default_MinimumLength', 'Please enter a minimum value of ' + this.model.FieldType.Type[this.currentType].Validation.MinimumValue + ' in Default Value.', (() => {
                    if (FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].DefaultValue)) {
                        if (FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].Validation.MinimumValue) && this.model.FieldType.Type[this.currentType].DefaultValue < this.model.FieldType.Type[this.currentType].Validation.MinimumValue) {
                            return true;
                        }
                    }

                    return false;
                })());
            }

            if (fieldname == '*' || fieldname == 'MaximumLength' || fieldname == 'DefaultValue') {
                this.setValidation('default_MaximumLength', 'Please enter a maximum value of ' + this.model.FieldType.Type[this.currentType].Validation.MaximumValue + ' in Default Value.', (() => {
                    if (FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].DefaultValue)) {
                        if (FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].Validation.MaximumValue) && +this.model.FieldType.Type[this.currentType].DefaultValue > this.model.FieldType.Type[this.currentType].Validation.MaximumValue) {
                            return true;
                        }
                    }
                    return false;
                })());
            }

            if (fieldname == '*' || fieldname == 'MinimumLength' || fieldname == 'MaximumLength') {
                this.setValidation('number_minmax', 'Please enter a minimum value which is lower than the maximum value.', (() => {
                    if (FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].Validation.MaximumValue) && FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].Validation.MaximumValue))
                        return (this.model.FieldType.Type[this.currentType].Validation.MinimumValue > this.model.FieldType.Type[this.currentType].Validation.MaximumValue);
                    return false;
                })());
            }
        }

        if (this.currentType == 'Text') {
            if (fieldname == '*' || fieldname == 'Pattern' || fieldname == 'DefaultValue') {
                this.setValidation('default_validationpattern', 'Default Value does not match Validation Pattern.', (() => {
                    if (this.model.FieldType.Type[this.currentType].Validation.Pattern > "" && this.model.FieldType.Type[this.currentType].DefaultValue > "") {
                        var patternRegex = new RegExp(this.model.FieldType.Type[this.currentType].Validation.Pattern);
                        return !patternRegex.test(this.model.FieldType.Type[this.currentType].DefaultValue);
                    }
                    return false;
                })());
            }

            if (fieldname == '*' || fieldname == 'MinimumLength') {
                this.setValidation('MinimumLength_integer', 'Please enter a valid integer for Minimum Value.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation && this.model.FieldType.Type[this.currentType].Validation.MinimumLength % 1 != 0);
                })());
                this.setValidation('MinimumLength_toolong', 'Please enter a Minimum Length shorter than ' + this.minLengthUpperNumeric + '.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation && this.model.FieldType.Type[this.currentType].Validation.MinimumLength > this.minLengthUpperNumeric);
                })());
                this.setValidation('MinimumLength_tooshort', 'Minimum Length must be a positive numnber.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation && this.model.FieldType.Type[this.currentType].Validation.MinimumLength < this.minLengthLowerText);
                })());
            }

            if (fieldname == '*' || fieldname == 'MaximumLength') {
                var m
                this.setValidation('MaximumLength_integer', 'Please enter a valid integer for Maximum Value.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation.MaximumLength && this.model.FieldType.Type[this.currentType].Validation.MaximumLength % 1 != 0);
                })());
                this.setValidation('MaximumLength_toolong', 'Please enter Maximum Length shorter than ' + this.maxLengthUpperText + '.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation.MaximumLength && this.model.FieldType.Type[this.currentType].Validation.MaximumLength > this.maxLengthUpperText);
                })());
                this.setValidation('MaximumLength_tooshort', 'Maximum Length must be a positive numnber.', (() => {
                    return (this.model.FieldType.Type[this.currentType].Validation.MaximumLength && this.model.FieldType.Type[this.currentType].Validation.MaximumLength < this.maxLengthLowerText);
                })());
            }

            if (fieldname == '*' || fieldname == 'MinimumLength' || fieldname == 'DefaultValue') {
                this.setValidation('default_MinimumLength_text', 'Default value is shorter than ' + this.model.FieldType.Type[this.currentType].Validation + '.', (() => {
                    if (this.model.FieldType.Type[this.currentType].DefaultValue) {
                        return (FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].Validation) && this.model.FieldType.Type[this.currentType].DefaultValue.length > 0 && this.model.FieldType.Type[this.currentType].DefaultValue.length < this.model.FieldType.Type[this.currentType].Validation.MinimumLength);
                    } else {
                        return false;
                    }
                })());
            }

            if (fieldname == '*' || fieldname == 'MaximumLength' || fieldname == 'DefaultValue') {
                this.setValidation('default_MaximumLength_text', 'Default value is longer than ' + this.model.FieldType.Type[this.currentType].Validation.MaximumLength + '.', (() => {
                    if (this.model.FieldType.Type[this.currentType].DefaultValue) {
                        return (FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].Validation.MaximumLength) && this.model.FieldType.Type[this.currentType].DefaultValue.length > this.model.FieldType.Type[this.currentType].Validation.MaximumLength);
                    } else {
                        return false;
                    }
                })());
            }

            if (fieldname == '*' || fieldname == 'MinimumLength' || fieldname == 'MaximumLength') {
                this.setValidation('MinimumLengthMaximumLength_text', 'Maximum Lenght is shorter than Minimum Length.', (() => {
                    if (this.model.FieldType.Type[this.currentType].Validation && FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].Validation.MaximumLength)) {
                        return (FormHelpers.isNumber(this.model.FieldType.Type[this.currentType].Validation.MaximumLength) && this.model.FieldType.Type[this.currentType].Validation.MinimumLength > this.model.FieldType.Type[this.currentType].Validation.MaximumLength);
                    } else {
                        return false;
                    }
                })());
            }

        }

        if (this.currentType == 'Lookup') {
            this.setValidation('AllowAllLabel_text', 'Please specify a label for ALL Value Selection.', (() => {
                if (this.model.FieldType.Type[this.currentType].AllowAllValue) {
                    return (this.model.FieldType.Type[this.currentType].AllowAllLabel == undefined || this.model.FieldType.Type[this.currentType].AllowAllLabel.length == 0);
                } else {
                    return false;
                }
            })());
        }

        this.errorMessage = Array.from(this.validationErrors.values()).join('\n');
    }

    private CheckMinRequired(fem: FieldTypeEditorModel) {
        if (!fem) {
            return;
        }
        if (this.currentType == 'Number' || this.currentType == 'Decimal') {
            return false;
        } else {
            return !fem.FieldType.Type[this.currentType].Validation.IsRequired;
        }
    }

    private updateApiName(event) {
        if (this.actionName == 'Edit')
            return;
        let nameValue: string = event.target.value.replace(/[^a-zA-Z0-9_]/g, '');
        this.model.FieldType.Name = nameValue.substring(0, 128);
        this.validate('NameTaken');
    }

    private addRelation(item: FieldTypeRelationItemEditorModel) {
        let i = new FieldTypeRelationItemEditorModel();
        let params = item.selectedRelationItemID.split('|');
        let assetTypeUid = params[1];
        let intersectType = params[0];

        i.AssetTypeUid = assetTypeUid;
        i.IntersectTypeUid = intersectType.toLocaleLowerCase();
        i.displayValue = item.relationItems.find(i => i.value == item.selectedRelationItemID).title;

        this.model.RelationItems.push(i);
        this.relationItemCount = this.model.RelationItems.length;
        this.loadRelationItems(this.relationItemCount - 1).subscribe();
    }

    private removeRelation(item: FieldTypeRelationItemEditorModel) {
        //only last item can be deleted
        this.model.RelationItems.pop();
        this.relationItemCount = this.model.RelationItems.length;
    }

    private anyDisplayFieldsSelected(e: any) {
        if (this.currentType != 'ComplexRelationLookup') {
            this.displayFieldSelected = true;

            if (this.lookups.Field_FieldFromRelRelationships.length > 0) {
                this.cardinalFieldFromRelationshipSelected(this.lookups.Field_FieldFromRelRelationships[0].value).subscribe();
            }

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

    public onDateSelectMethod(e: Date) {
        if (this.currentType == "Date" || this.currentType == "DateTime") {
            this.model.FieldType.Type[this.currentType].DefaultValue;
        }
    }

    private getGovernDate(e: Date) {
        if (e === null || e === undefined) {
            return "";
        }

        return (e.getMonth() + 1) + '/' + e.getDate() + '/' + e.getFullYear();
    }

    public isRelationshipWithMultipleCardinality(): boolean {
        return true;
    }

    private isSettingDisabled(val: string) {

        if (this.objectType == 'TaskType') {
            if (this.name == 'Name') return true;
            if ((this.name == 'StepNo' || this.name == 'GovernanceRole') && (val != 'IsEditable' && val != 'IsRequired' && val != 'SearchAddToResult')) {
                return true;
            }
            var staticFields: string[] = [];
            staticFields.push('Name');
            staticFields.push('GovernanceRole');
            staticFields.push('StepNo');

            if (!staticFields.some(x => x == this.name)) {
                if (val == 'IsListable' || val == 'IsPartOfKey' || val == 'IsPrimaryFilter') return true;
            }
        }

        switch (val) {
            case 'IsDisplayable':
                return (['ComplexRelationLookup', 'RefListRelationship'].indexOf(this.currentType) > -1);
            case 'IsEditable':
                return (['ComplexRelationLookup', 'FieldFromRelationship', 'Json', 'JSON', 'JsonElement', 'OwnershipLookup', 'Path', 'RefListRelationship', 'Tag', 'Score', 'Counter'].indexOf(this.currentType) > -1);
            case 'IsListable':
                return (['ComplexRelationLookup', 'RefListRelationship', 'Json', 'JSON'].indexOf(this.currentType) > -1
                    || (this.currentType == 'Relationship' && !this.isListableRelationship));
            case 'IsRequired':
                    return (['ComplexRelationLookup', 'FieldFromRelationship', 'Json', 'JSON', 'JsonElement', 'OwnershipLookup', 'Path', 'RefListRelationship', 'Relationship', 'Tag', 'Score', 'Counter'].indexOf(this.currentType) > -1);
            case 'IsPartOfKey':
                return (['ComplexRelationLookup', 'FieldFromRelationship', 'Json', 'JSON', 'JsonElement', 'OwnershipLookup', 'Path', 'RefListRelationship', 'Relationship', 'Tag', 'Score', 'Link']
                    .indexOf(this.currentType) > -1
                    || (this.model.FieldType.Type
                        && this.model.FieldType.Type[this.currentType].List
                        && this.model.FieldType.Type[this.currentType].List.AllowMultipleValues)
                    || this.objectType == 'ReferenceItemType');
            case 'IsPrimaryFilter':
                return (!this.supportsPrimaryFilterOption || ['FieldFromRelationship', 'ComplexRelationLookup', 'OwnershipLookup', 'Json', 'JSON', 'JsonElement', 'Path', 'RefListRelationship'].indexOf(this.currentType) > -1);
            case 'AllowMultipleValues':
                return (['Lookup'].indexOf(this.currentType) == -1);
            case 'ShowIfEmpty':
                return (['Path', 'Tag'].indexOf(this.currentType) > -1 || (this.currentType == 'Score' && !this.model.FieldType.Type['Score'].IsDisplayable));
            case 'SearchAddToResult':
                return (['Path', 'Html', 'Json', 'JSON', 'JsonElement', 'OwnershipLookup', 'ComplexRelationLookup', 'RefListRelationship', 'Score', 'Tag'].indexOf(this.currentType) > -1);
            case 'isSettingDisabled':
                return (['Json', 'JSON', 'JsonElement', 'ComplexRelationLookup', 'Tag', 'RefListRelationship'].indexOf(this.currentType) > -1);
            case 'DisplayInColumn':
                if (this.currentType === "OwnershipLookup") {
                    var isDisabled = !this.model.FieldType.Type[this.currentType].Definition.DisplayAsList;
                    if (isDisabled) {
                        this.model.FieldType.Type[this.currentType].DisplayInColumn = false;
                    }
                    return isDisabled;
                }
                return false;
            default:
                console.warn(`invalid setting [${val}] passed to isSettingDisabled`);
        }
    }

    //need this to keep the UI behavior of grouping the fields with their relationships
    ConvertDisplayFieldsToAPIDefinition() {
        if (!this.model.RelationItems || this.model.RelationItems.length < 1)
            return;
        var definitionArray: Relation[] = [];
        var fieldsArray: DefinitionField[] = [];
        this.model.RelationItems.forEach((x, i) => {
            let definition = {
                IntersectTypeUid: x.IntersectTypeUid,
                AssetTypeUid: x.AssetTypeUid,
                RelationType: null, //deprecated
                Direction: Direction[x.Direction]
            };

            let mappedFields: DefinitionField[] = x.DisplayFields.filter(xf => xf.Show || xf.Filter !== '' || xf.SortOrder).map((f) => {
                return {
                    AssetTypeUid: x.AssetTypeUid,
                    FieldTypeName: f.FieldTypeName,
                    Filter: f.Filter,
                    OverrideDisplayName: f.OverrideDisplayName,
                    DisplayOrder: f.DisplayOrder,
                    SortOrder: f.SortOrder,
                    Show: f.Show,
                    Width: f.Width,
                    RelationIndex: i
                };
            });
            definitionArray.push(definition);
            fieldsArray.push(...mappedFields);
        });
        this.model.FieldType.Type.ComplexRelationLookup.Definition.Relations = definitionArray;
        this.model.FieldType.Type.ComplexRelationLookup.Definition.Fields = fieldsArray;
    }

    checkCurrentTypeName(name: string): string {
        if (this.currentType == 'ComputedRelationshipField')
            return "FieldFromRelationship";
        if (this.currentType == "ComputedOwnershipLookup")
            return "OwnershipLookup";
        if (this.currentType == "ComputedRelationshipReferenceList")
            return "RefListRelationship";
        if (this.currentType == "ComputedRelationshipLookup")
            return "ComplexRelationLookup";
        if (this.currentType == "Json")
            return "JSON";
        return name;
    }

    onShowDetailChange(event: boolean) {
        if (event === false && this.currentType === 'Score') {
            this.model.FieldType.Type[this.currentType].ShowIfEmpty = false;
        }
        if (event === false && this.model.FieldType.Type[this.currentType].IsDisplayable) {
            this.model.FieldType.Type[this.currentType].IsDisplayable = false;
        }
    }

    onShowEditableChange(event: boolean) {
        if (this.currentType == 'Relationship') {
            if (event == true) {
                this.showDescription = true;
            }
            else {
                this.showDescription = false;
                this.model.FieldType.Type[this.currentType].Description.Form = "";
            }
        }
    }

    onAddToSearchChange(event: boolean) {
        if (!event) {
            this.model.FieldType.Type[this.currentType].Search.Prefix = null;
            this.model.FieldType.Type[this.currentType].Search.Suffix = null;
            this.model.FieldType.Type[this.currentType].Search.DisplayOrder = null;
        }
    }

    onEnableListSingleResponsibilityType(event: boolean) {
        this.enableListSingleResponsibilityType = event;
        if (!event) {
            this.model.FieldType.Type[this.currentType].Definition.ResponsibilityTypeUid = null;
        }
    }

    onDisplayAsList(event: boolean) {
        if (event) {
            this.model.FieldType.Type[this.currentType].Definition.DisplayAssignmentSource = false;
            this.model.FieldType.Type[this.currentType].HideFilter = false;
            this.model.FieldType.Type[this.currentType].HideFooter = false;
            this.model.FieldType.Type[this.currentType].HideHeader = false;
        }
    }

    public getSelectResponsibilityTypePlaceholder() {
        //Using a string with space, because if empty string is returned, p-dropdown behaves like there is no placeholder
        return this.enableListSingleResponsibilityType ? "Value Required" : " ";
    }

    private isValidationPatternValid() {
        if (this.currentType == 'Text') {
            var pattern = this.model.FieldType.Type[this.currentType].Validation.Pattern;

            if (((typeof pattern) != "undefined") && pattern !== null && pattern.length > 0) {
                try {
                    new RegExp(pattern);
                }
                catch (e) {
                    return false
                }
            }
        }

        return true;
    }
}
