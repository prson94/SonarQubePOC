import * as _ from 'lodash';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChange,

    ViewChild,
    ElementRef
} from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';

import { EditorCategory, EditorField, EditorRow } from '../../../models/editor-field.model';

import { EditorDefinitionService } from '../../../services/editor-definition.service';
import { UriBasedService } from '../../../services/uri-based.service';
import { CascadeService } from '../../../services/cascade.service';

import { BaseComponent } from '../base.component';

import { FormHelpers } from '../../../static/form-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetEditorModel } from '../../../models/asset.model';
import { AssetService } from '../../../services/asset.service';
import { Subject } from 'rxjs';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-dynamic-editor',
    templateUrl: './dynamic-editor.component.html',
    providers: [EditorDefinitionService, UriBasedService, CascadeService, AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class DynamicEditorComponent extends BaseComponent implements OnChanges, OnInit {
    @Input() selection: any;
    @Input() rowID: string = 'ID';
    @Input() title: string;
    @Input() directions: string;
    @Input() objectID: number = 0;
    @Input() objectTypeUid: string;
    @Input() targetUid: string;

    @Input() assetUid: string;
    @Input() parentID: number;
    @Input() objectType: string;
    @Input() createUri: string;
    @Input() createParams: any[] = [];
    @Input() editUri: string;
    @Input() editParams: any[] = [];
    @Input() targetType: string;
    @Input() targetTypeID: number;
    @Input() hasCloseButton = false;
    @Input() newActionName: string = "New";
    @Input() hasHeader = true;
    @Input() selectedObject: string;
    @Input() selectedObjectID: any;
    @Input() adding: boolean = false;
    @Input() isV2API: boolean = false;
    @Input() useV2ApiLink: boolean = false;

    @Input() useTypeUidForDefinition: boolean = false;
    @Input() showActions: boolean = true;

    @Input() useModelBinding: boolean = false;
    @Input() dataModel: any = null;

    @Input() isSaveDisabled: boolean = false;

    @Output() modelChanged = new EventEmitter();
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    //Modal
    @Input() showAsModal: boolean = false;
    @Input() modalTitle: string = '';
    @Input() isModalVisible: boolean = false;
    @Input() useNonLegacyData: boolean = false;
    private savingInProgress: boolean = false;
    private consolidateToTag: any;
    private isInError: boolean = false;
    private isInErrorMessage: string = "";
    readonly defaultCategory: string = "General";

    form: FormGroup;

    action: string = "Edit";
    fields: EditorField[] = [];

    categories: EditorCategory[] = [];
    editedItem: any;
    editorChange: Subject<any> = new Subject();
    hasDirections: boolean = false;
    hasIconFields = false;
    fore: EditorField;
    back: EditorField;
    selectedTagID: number;
    @ViewChild('assetForm', { static: false }) formElement: ElementRef;

    constructor(
        private ref: ChangeDetectorRef,
        private formBuilder: FormBuilder,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private editorDefinitionService: EditorDefinitionService,
        private uriBasedService: UriBasedService,
        private cascadeService: CascadeService,
        private assetService: AssetService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.hasDirections = (this.directions && this.directions !== "");
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['objectID']) {
            if (!changes['objectID'].isFirstChange() && (changes['objectID'].previousValue != changes['objectID'].currentValue)) { // object has changed            
                this.load();
            }
        }
        if (changes['objectTypeUid']) {
            if (!changes['objectTypeUid'].isFirstChange() && (changes['objectTypeUid'].previousValue != changes['objectTypeUid'].currentValue)) { // object has changed            
                this.load();
            }
        }
        if (changes['assetUid']) {
            if (!changes['assetUid'].isFirstChange() && (changes['assetUid'].previousValue != changes['assetUid'].currentValue)) { // object has changed            
                this.load();
            }
        }
        if (changes['isModalVisible']) {
            if (!changes['isModalVisible'].isFirstChange() && (changes['isModalVisible'].previousValue != changes['isModalVisible'].currentValue)) { // visibility has changed            
                this.savingInProgress = false;
                this.consolidateToTag = null;
                this.load();
            }
        }
    }
    autoCompleteSelected(event) {
        if (this.objectType == 'Tag' && !this.adding) {
            this.consolidateToTag = event;
        } else if (this.objectType == 'Tag' && this.adding) {
            if (event) {
                this.consolidateToTag = null;
                this.selectedTagID = event.ID;
            }

        }

    }

    focusToFirst() {
        if (this.formElement) {
            this.formElement.nativeElement.querySelector("input:not([type='hidden'])").focus();
        }
    }

    private load() {
        this.isInErrorMessage = '';
        this.isInError = false;
        if (this.selection != undefined) {
            this.editedItem = _.cloneDeep(this.selection);
        } else {
            this.editedItem = {};
            this.action = this.newActionName;
        }
        this.getDefinition();
    }

    getDefinition() {
        let id = (this.selection ? this.selection[this.rowID] : null);

        if (this.selection) {
            if (this.objectType == 'IntersectType' || this.objectType == 'Predicate' || this.objectType == 'IssueType')
                id = this.selection.Uid;

            if (this.selection.uid)
                id = this.selection.uid;

            //For ones recieved from GET V2 Asset API
            if (this.isV2API && this.selection.AssetUid) {
                id = this.selection.AssetUid;
            }
        }

        this.isLoading = true;

        if (this.objectType === "IntersectType" && this.action === "Edit") {
            this.useTypeUidForDefinition = false;
        }

        if (this.useTypeUidForDefinition) {
            this.editorDefinitionService.getEditorDefinitionUid(this.objectTypeUid, this.objectType, this.targetUid)
                .subscribe(result => {
                    this.handleEditor(result);
                });
        }
        else if (this.useNonLegacyData) {
            this.editorDefinitionService.getEditorDefinitionNonLegacy(this.objectTypeUid, this.assetUid)
                .subscribe(result => {
                    this.handleEditor(result);
                });
        }
        else {
            this.editorDefinitionService.getEditorDefinition(
                id,
                this.objectID,
                this.objectType,
                this.parentID,
                this.targetType,
                this.targetTypeID,
                this.createParams,
                this.editParams,
                this.action
            ).subscribe(result => {
                this.handleEditor(result);
            });
        }
    }

    handleEditor(result: EditorField[]) {

        if (this.dataModel && !this.assetUid) {
            result.forEach(res => {
                if (res.Name == 'Name') {
                    res.Value = this.dataModel['Name'];
                }
            });
        }

        if ((result as any).type && (result as any).type == "error") {
            this.isInErrorMessage = (result as any).message;
            this.isInError = true;
            this.isLoading = false;
        }
        else {
            this.isInErrorMessage = '';
            this.isInError = false;
            let currentCategory = null;
            
            this.categories = [];

            result = _.orderBy(result, [field => field.Row ? field.Row : 0], ['asc']);
            this.fields = result;

            this.fields.forEach(f => {

                if (f.Category == null) {
                    currentCategory =  "";
                }
                else {
                    currentCategory = f.Category;
                }


                if (this.categories.findIndex(dc => dc.name == currentCategory) == -1) {
                    let category = new EditorCategory();
                    category.name = currentCategory;
                    category.rows = [];
                    if (currentCategory == "") {
                        this.categories.unshift(category);
                    }
                    else {
                        this.categories.push(category);
                    }

                }


                if (f.FieldType && f.FieldType.toUpperCase() == 'BOOLEAN' && f.Value != null) {
                    if (f.Value) {
                        /* checkbox doesnt work binding to a string */
                        f.Value = (f.Value.toUpperCase() == "TRUE" ? true : false);
                    }
                    else {
                        f.Value = false;
                    }
                }

                let curCategory = this.categories.find(dc => dc.name == currentCategory);

                let r = curCategory.rows.find(r => r.Row == (f.Row || 0));
                if (r) {
                    r.Fields.push(f);
                } else {
                    let n = new EditorRow();

                    n.Row = f.Row;
                    n.Fields.push(f);
                    curCategory.rows.push(n);
                }
            });

            this.categories = this.categories.sort((a, b) => {
                if (b.name === this.defaultCategory && !a.name) {
                    return -1;
                }
                else if (a.name === this.defaultCategory && !b.name) {
                    return 1;
                }
                else if (!a.name || a.name === this.defaultCategory) {
                    return -1;
                }
                else if (!b.name|| b.name === this.defaultCategory) {
                    return 1;
                }
                return 0;
            });


            this.fore = this.fields.find(f => f.FieldType == 'Color' && f.FieldName == 'IconForeColor');
            this.back = this.fields.find(f => f.FieldType == 'Color' && f.FieldName == 'IconBackColor');

            if (this.fore != null && this.back != null) {
                this.hasIconFields = true;
            }

            this.form = this.toFormGroup(this.fields);
            if (this.useModelBinding) {
                this.form.valueChanges.subscribe(x => {
                    this.onSubmit();
                })
            }
        }

        this.ref.markForCheck();
        this.isLoading = false;
        setTimeout(() => {
            this.focusToFirst();
        }, 200);
    }

    toFormGroup(editorField: EditorField[]) {
        let group: any = {};

        editorField.forEach(field => {
            //if its a link we need to add two fields a link and name            
            if (field.FieldType == "Link") {
                let parts = (field.Value ? field.Value.split("|") : []);
                let url = "";
                let name = "";


                if (parts.length == 2) {
                    name = parts[0];
                    url = parts[1];
                } else if (field.Value) {
                    name = '';
                    url = field.Value;
                }

                group[field.FieldName + '_Name'] = new FormControl(name || '');
                group[field.FieldName + '_Url'] = new FormControl(url || '', this.getFieldValidators(field));
            }
            else if (field.FieldType == "DateTime" || field.FieldType == "Date") {
                if (field.Value != null) {
                    let date = new Date(field.Value);
                    field.Value = date;
                }

                group[field.FieldName] = new FormControl({
                    value: (field.Value),
                    disabled: field.ReadOnly
                }, this.getFieldValidators(field));
            }
            else {
                if (field.FieldType == "Relationship" && this.selection) {
                    if (field.Value != null) {
                        field.Value = JSON.parse(field.Value);
                    }
                } else if (field.FieldType == "Lookup" && !field.Value && this.selection) {
                    let selected = field.Items.filter(x => x.Selected);

                    field.Value = [];

                    for (let item of selected) {
                        field.Value.push(item.Value);
                    }

                    if (field.Value.length == 0) {
                        field.Value = null;
                    }
                } else if (field.FieldType == "Lookup" && field.Value) {
                    if (field.Value != null && field.MultiSelect && typeof field.Value === "string") {
                        field.Value = field.Value.split(',');
                    }
                }
                var setDisabled = field.ReadOnly;
                if (field.FieldType == "Lookup" && !field.Value && field.DelayedLoadType == 'FieldFilter') {
                    setDisabled = true;
                }

                var fieldValue = field.Value;
                if (field.FieldType != 'Boolean') {
                    fieldValue = field.Value === null ? '' : field.Value;
                }
                else if (field.FieldType == 'Boolean' && field.Value == null) {
                    fieldValue = undefined;
                }


                group[field.FieldName] = new FormControl({
                    value: fieldValue,
                    disabled: setDisabled
                }, this.getFieldValidators(field));
            }
        });

        return new FormGroup(group);
    }

    private getFieldValidators(field: EditorField) {
        var validators = [];
        let minLen = Number.MIN_SAFE_INTEGER;
        let maxLen = Number.MAX_SAFE_INTEGER;

        if (field.Validations) {
            for (let validation of field.Validations) {
                if (validation.rule && validation.rule.startsWith('length=')) {
                    var vals = validation.rule.split(',');

                    if (vals.length === 2) {
                        maxLen = +vals[1];

                        if (field.FieldType === 'Number' || field.FieldType === 'Decimal') {
                            validators.push(Validators.max(maxLen));
                        } else {
                            validators.push(Validators.maxLength(maxLen));
                        }

                        var minParts = vals[0].split('=');
                        if (minParts.length == 2) {
                            minLen = +minParts[1];

                            if (minLen > 1) {
                                if (field.FieldType === 'Number' || field.FieldType === 'Decimal') {
                                    validators.push(Validators.min(minLen));
                                } else {
                                    // only min length > 1
                                    validators.push(Validators.minLength(minLen));
                                }
                            }
                        }
                    }
                } else if (validation.rule && validation.rule.startsWith('required')) {
                    validators.push(Validators.compose([Validators.required]));
                } else if (validation.rule && validation.rule.startsWith('minLength=')) {
                    minLen = +validation.rule.split('=').pop();

                    if (field.FieldType === 'Number' || field.FieldType === 'Decimal') {
                        validators.push(Validators.min(minLen));
                    } else {
                        validators.push(Validators.minLength(minLen));
                    }
                } else if (validation.rule && validation.rule.startsWith('maxLength=')) {
                    maxLen = +validation.rule.split('=').pop();

                    if (field.FieldType === 'Number' || field.FieldType === 'Decimal') {
                        validators.push(Validators.max(maxLen));
                    } else {
                        validators.push(Validators.maxLength(maxLen));
                    }

                } else if (validation.regex) {
                    validators.push(Validators.pattern(validation.regex));
                }
                validators.push();
            }
        }

        if (field.Required) {
            validators.push(Validators.required);
        }

        if (field.FieldType === 'Number') {
            validators.push(FormHelpers.integerValidator);

            if (validators.indexOf(Validators.min) == -1) {
                validators.push(Validators.min(minLen));
            }

            if (validators.indexOf(Validators.max) == -1) {
                validators.push(Validators.max(maxLen));
            }
        }
        if (field.FieldType === 'Decimal') {
            validators.push(FormHelpers.numberValidator);

            if (validators.indexOf(Validators.min) == -1) {
                validators.push(Validators.min(minLen));
            }

            if (validators.indexOf(Validators.max) == -1) {
                validators.push(Validators.max(maxLen));
            }
        }

        return validators.length > 0 ? validators : null;
    }

    public pad(s): string { return (s < 10) ? '0' + s : s; }

    onSubmit() {
        this.savingInProgress = true;
        let action = (this.selection == null ? "new" : "edit");
        let values: any = {};

        //adjust any dates to utc
        for (var p in this.form.value) {
            if (this.form.value.hasOwnProperty(p)) {
                let field = this.fields.find(f => f.FieldName == p);

                if (this.form.value[p] instanceof Date) {
                    if (field != null && field.FieldType == 'Date' && this.isV2API) {
                        let simpleDate = [this.pad(this.form.value[p].getMonth() + 1), this.pad(this.form.value[p].getDate()), this.pad(this.form.value[p].getFullYear())].join('/');
                        this.form.value[p] = simpleDate;
                    }
                    else if (field != null && field.FieldType == 'DateTime' && this.isV2API) {
                        if (this.form.value[p] != 'Invalid Date')
                            this.form.value[p] = new Date(this.form.value[p]).toISOString();
                    }
                    else {
                        this.form.value[p] = this.getUTCDate(this.form.value[p]);
                    }
                } else if (field != null && field.FieldType == 'Lookup' && field.UseTypeahead) {
                    if (this.form.value[p] != null && this.form.value[p].Value) {
                        this.form.value[p] = this.form.value[p].Value;
                    }
                }
            }
        }


        //takes the form and convert any array values to , separated string values
        for (var p in this.form.value) {
            if (this.form.value.hasOwnProperty(p)) {
                if (Array.isArray(this.form.value[p])) {
                    values[p] = this.form.value[p].join();
                } else {
                    if (this.form.value[p] === undefined && this.fields.filter(x => x.FieldName == p && x.FieldType == 'Boolean').length > 0)
                        values[p] = null;
                    else
                        values[p] = this.form.value[p];
                }
            }
        }

        // if this is the v2 api we need to combine any link field types into the format stored in the db
        // tallyfy|https://tallyfy.com/what-is-compliance-management/
        if (this.isV2API || this.useV2ApiLink) {
            let links = this.fields.filter(x => x.FieldType == 'Link');
            //need to get the link and url for each            
            for (let link of links) {
                let url = values[link.FieldName + '_Url'];
                delete values[link.FieldName + '_Url'];
                let name = values[link.FieldName + '_Name'];
                delete values[link.FieldName + '_Name'];
                //No name and url, use empty string rather than '|'
                values[link.FieldName] = (name == '' && url == '') ? `` : `${name}|${url}`;
            }

        }

        //Replace empty string value with null to properly delete field from database and avoid validation parsing errors
        Object.keys(values).forEach(key => {
            if (values[key] === '')
                values[key] = null;
        })

        //when using model binding onSubmit() is called on every change, but just emit form values, do not call save api (used on Process Designer)
        if (this.useModelBinding) {
            this.modelChanged.emit({ values: values, fields: this.fields });
            return;
        }

        if ((this.createUri && action == "new") || (this.editUri && action == "edit")) {
            this.isLoading = true;

            this.uriBasedService.saveItem(this.createUri, this.editUri, values)
                .subscribe(result => {
                    this.showMessageForResult(this.messagesService, result);
                    this.isLoading = false;
                    this.saveClick.emit({ item: result, action: action, values: values });
                });
        } else {
            if (this.consolidateToTag) {
                this.saveClick.emit({ item: values, action: action, additionalOption: this.consolidateToTag });

            }
            else if (this.isV2API) {
                this.postToApiV2({ item: values, action: action });
            }
            else {
                this.saveClick.emit({ item: values, action: action });
            }
        }
    }

    postToApiV2(event) {
        this.isLoading = true;
        let values: any = {};
        let asset: AssetEditorModel = new AssetEditorModel();
        asset.Fields = {};

        //takes the form and convert any array values to , separated string values
        for (var p in event.item) {
            if (event.item.hasOwnProperty(p)) {
                if (Array.isArray(event.item[p])) {
                    values[p] = event.item[p].join();
                } else {
                    values[p] = event.item[p];
                }
            }
        }

        //convert to an asset
        for (var p in values) {
            if (p.toUpperCase() == "PARENTUID") {
                asset.ParentUid = values[p];
            }
            else if (p.toUpperCase() == "UID") {
                asset.Uid = values[p];
            }
            else if (p.toUpperCase() == "ASSETTYPEUID") {
                //ignore
            }
            else {
                if (values[p] == undefined) {
                    asset.Fields[p] = null;
                }
                else {
                    asset.Fields[p] = values[p];
                }
            }
        }
        this.assetService.saveAsset(this.objectTypeUid, asset)
            .subscribe(res => {
                event.Success = res.Success;

                if (res.Success) {
                    let msg = asset.Uid ? 'Successfully updated' : 'Successfully added';
                    this.showMessageForApiResult(this.messagesService, res, msg);
                    if (res.uid) {
                        event.assetUid = res.uid;
                        event.assetTypeUid = this.objectTypeUid;
                    }
                    this.saveClick.emit(event);
                }
                else {
                    this.showMessageForApiResult(this.messagesService, res);
                    this.isLoading = false;
                    this.saveClick.emit(event);
                }

            });
    }

    getUTCDate(date: Date): Date {
        date.setMinutes(date.getMinutes() - date.getTimezoneOffset());

        return date;
    }

    listSelectionChanged(event: any) {
        //look for any fields with this as a parent        
        var field = event.field;
        var value = event.value;

        if (field == null || this.fields == null || this.fields.length <= 0) {
            return;
        }

        if (this.objectType == "ExportTemplate" && field.Name == "Asset Type") {
            var item = field.Items.filter(x => {
                return x.Value == field.Value
            })[0];
            if (item && item.Text.startsWith("Rule")) {
                this.fields.find(x => x.FieldName == "IncludeParent").FieldType = "no-display";
                this.fields.find(x => x.FieldName == "IncludeParent").Value = false;
                for (var p in this.form.value) {
                    if (this.form.value.hasOwnProperty(p)) {
                        if (p == "IncludeParent") {
                            this.form.controls[p].setValue(false);
                        }
                    }
                }
                this.ref.detectChanges();
            } else {
                this.fields.find(x => x.FieldName == "IncludeParent").FieldType = "Boolean";
                this.ref.markForCheck();
            }
        }
        if (field.FieldType == 'Relationship' && field.IsSemantic === true) {
            this.editorChange.next(event);
        }

        if (Array.isArray(event.value)) {
            value = event.value.join();
        }

        this.fields.forEach(editorField => {
            if (editorField.ParentFieldTypeID == field.FieldTypeID) {
                this.cascadeService.cascadeEvent(editorField.FieldTypeID, value);
            }
        });
    }

    useAccordion(category: any): boolean {
        if (category == null || !category.name) {
            return false;
        }
        if (category.name === this.defaultCategory) {
            return false;
        }

        return true;
    }
}
