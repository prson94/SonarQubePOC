import * as _ from 'lodash';
import { Number, setTimeout } from 'core-js';
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
import { FieldsService } from '../../../services/fields.service';
import { CascadeService } from '../../../services/cascade.service';

import { BaseComponent } from '../base.component';

import { FormHelpers } from '../../../static/form-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { concat } from 'rxjs';
import { forEach } from '@angular/router/src/utils/collection';

@Component({
    selector: 'd3s-dynamic-editor',
    templateUrl: './dynamic-editor.component.html',
    providers: [EditorDefinitionService, UriBasedService, FieldsService, CascadeService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class DynamicEditorComponent extends BaseComponent implements OnChanges, OnInit {
    @Input() selection: any;
    @Input() rowID: string = 'ID';
    @Input() title: string;
    @Input() directions: string;
    @Input() objectID: number = 0;
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
    @Input() copy: boolean;
    @Input() selectedObject: string;
    @Input() selectedObjectID: number;
    @Input() adding: boolean = false;
    @Input() isV2API: boolean = false;

    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    //Modal
    @Input() showAsModal: boolean = false;
    @Input() modalTitle: string = '';
    @Input() isModalVisible: boolean = false;
    private savingInProgress: boolean = false;
    private consolidateToTag: any;

    form: FormGroup;

    action: string = "Edit";
    fields: EditorField[] = [];

    categories: EditorCategory[] = [];
    editedItem: any;
    hasDirections: boolean = false;
    hasIconFields = false;
    fore: EditorField;
    back: EditorField;
    selectedTagID: number;
    @ViewChild('assetForm') formElement: ElementRef;

    constructor(
        private ref: ChangeDetectorRef,
        private formBuilder: FormBuilder,
        private messagesService: MessagesObservableService,
        private editorDefinitionService: EditorDefinitionService,
        private uriBasedService: UriBasedService,
        private fieldsService: FieldsService,
        private cascadeService: CascadeService
    ) {
        super();
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
        if (this.formElement)
            this.formElement.nativeElement.querySelector("input:not([type='hidden'])").focus();
    }

    private load() {
        if (this.selection != undefined) {
            this.editedItem = _.cloneDeep(this.selection);
            this.action = this.copy ? "Copy" : this.action;
        } else {
            this.editedItem = {};
            this.action = this.newActionName;
        }
        this.getDefinition();
    }

    getDefinition() {
        let id = (this.selection ? this.selection[this.rowID] : null);
        if (this.objectType == 'IntersectType' && this.selection) {
            id = this.selection.Uid;
        }

        this.isLoading = true;

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
        ).subscribe(
            result => {
                let previousCategory = null;
                let currentCategory = null;
                let rows = [];
                let firstRow = true;

                this.isLoading = false;
                this.categories = [];

                result = _.orderBy(result, [field => field.Category ? field.Category.toLowerCase() : ''], ['asc']);
                this.fields = result;

                this.fields.forEach(f => {
                    if (this.copy == true && f.FieldName == "Name") {
                        f.Value = "";
                    }

                    currentCategory = f.Category;

                    if (firstRow) {
                        previousCategory = f.Category;
                        firstRow = false;
                    }

                    if (previousCategory != currentCategory) {
                        let category = new EditorCategory();
                        category.name = previousCategory;
                        category.rows = rows;
                        this.categories.push(category);
                        previousCategory = currentCategory;
                        rows = [];
                    }

                    if (f.FieldType && f.FieldType.toUpperCase() == 'BOOLEAN') {
                        if (f.Value) {
                            /* checkbox doesnt work binding to a string */
                            f.Value = (f.Value.toUpperCase() == "TRUE" ? true : false);
                        }
                        else {
                            f.Value = false;
                        }
                    }

                    let r = rows.find(r => r.Row == (f.Row || 0));
                    if (r) {
                        r.Fields.push(f);
                    } else {
                        let n = new EditorRow();

                        n.Row = f.Row;
                        n.Fields.push(f);
                        rows.push(n);
                    }
                });

                let category = new EditorCategory();
                category.name = currentCategory;
                category.rows = rows;
                this.categories.push(category);

                this.fore = this.fields.find(f => f.FieldType == 'Color' && f.FieldName == 'IconForeColor');
                this.back = this.fields.find(f => f.FieldType == 'Color' && f.FieldName == 'IconBackColor');

                if (this.fore != null && this.back != null) {
                    this.hasIconFields = true;
                }

                this.form = this.toFormGroup(this.fields);
                this.ref.markForCheck();
                setTimeout(() => {
                    this.focusToFirst();
                }, 200);
            });
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
            } else if (field.FieldType == "Date" || field.FieldType == "DateTime") {
                if (field.Value != null) {
                    field.Value = new Date(field.Value);
                }

                group[field.FieldName] = new FormControl({
                    value: (field.Value),
                    disabled: field.ReadOnly
                }, this.getFieldValidators(field));
            } else {
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

                group[field.FieldName] = new FormControl({
                    value: (field.Value === null ? '' : field.Value),
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

                    if (vals.length == 2) {
                        maxLen = +vals[1];

                        if (field.FieldType == 'Number' || field.FieldType == 'Decimal') {
                            validators.push(Validators.max(maxLen));
                        } else {
                            validators.push(Validators.maxLength(maxLen));
                        }

                        var minParts = vals[0].split('=');
                        if (minParts.length == 2) {
                            minLen = +minParts[1];

                            if (minLen > 1) {
                                if (field.FieldType == 'Number' || field.FieldType == 'Decimal') {
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

                    if (field.FieldType == 'Number' || field.FieldType == 'Decimal') {
                        validators.push(Validators.min(minLen));
                    } else {
                        validators.push(Validators.minLength(minLen));
                    }
                } else if (validation.rule && validation.rule.startsWith('maxLength=')) {
                    maxLen = +validation.rule.split('=').pop();

                    if (field.FieldType == 'Number' || field.FieldType == 'Decimal') {
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

        if (field.FieldType == 'Number') {
            validators.push(FormHelpers.integerValidator);

            if (validators.indexOf(Validators.min) == -1) {
                validators.push(Validators.min(minLen));
            }

            if (validators.indexOf(Validators.max) == -1) {
                validators.push(Validators.max(maxLen));
            }
        }
        if (field.FieldType == 'Decimal') {
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

    public pad(s) :string { return (s < 10) ? '0' + s : s; }

    onSubmit() {
        
        this.savingInProgress = true;

        let action = (this.selection == null ? "new" : "edit");
        let values: any = {};
        if (this.copy == true) {
            action = this.action;
        }


        //adjust any dates to utc
        for (var p in this.form.value) {
            if (this.form.value.hasOwnProperty(p)) {
                let field = this.fields.find(f => f.FieldName == p);

                if (this.form.value[p] instanceof Date) {
                    if (field != null && field.FieldType == 'Date' && this.isV2API) {
                        
                        let simpleDate = [this.pad(this.form.value[p].getMonth()+1), this.pad(this.form.value[p].getDate()), this.pad(this.form.value[p].getFullYear())].join('/');
                        this.form.value[p] = simpleDate;
                        console.log(simpleDate);
                    }
                    else {
                        this.form.value[p] = this.getUTCDate(this.form.value[p]);
                    }                    
                } else if (field != null && field.FieldType == 'Lookup' && field.UseTypeahead) {
                    if (this.form.value[p] != null) {
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
                    values[p] = this.form.value[p];
                }
            }
        }

        console.log(this.isV2API);

        // if this is the v2 api we need to combine any link field types into the format stored in the db
        // tallyfy|https://tallyfy.com/what-is-compliance-management/
        if (this.isV2API) {
            let links = this.fields.filter(x => x.FieldType == 'Link');            
            //need to get the link and url for each            
            for (let link of links) {                
                let url = values[link.FieldName + '_Url'];
                delete values[link.FieldName + '_Url'];
                let name = values[link.FieldName + '_Name'];
                delete values[link.FieldName + '_Name'];
                values[link.FieldName] = `${name}|${url}`;
            }

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
            else {
                this.saveClick.emit({ item: values, action: action });
            }
        }
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

        if (Array.isArray(event.value)) {
            value = event.value.join();
        }

        this.fields.forEach(editorField => {
            if (editorField.ParentFieldTypeID == field.FieldTypeID) {
                this.cascadeService.cascadeEvent(editorField.FieldTypeID, value);
            }
        });
    }
}
