import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { FormArray, FormGroup, FormBuilder, Validators, FormControl, ValidatorFn, AbstractControl } from '@angular/forms';
import { EditorDefinitionService } from '../../../services/editor-definition.service';
import { UriBasedService } from '../../../services/uri-based.service';
import { MessagesService } from '../../../services/messages.service';
import { FieldsService } from '../../../services/fields.service';
import { CascadeService } from '../../../services/cascade.service';
import { EditorField, EditorRow, FieldValidation, EditorDropDownItem, EditorCategory } from '../../../models/editor-field.model';
import { BaseComponent } from '../base.component';
import { FormHelpers } from '../../../static/form-helpers';

import * as _ from 'lodash';
import { max } from 'rxjs/operators';
import { Number } from 'core-js';

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

    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    form: FormGroup;

    action: string = "Edit";
    fields: EditorField[] = [];

    categories: EditorCategory[] = [];
    editedItem: any;

    hasIconFields = false;
    fore: EditorField;
    back: EditorField;

    constructor(private ref: ChangeDetectorRef,
        private formBuilder: FormBuilder,
        private messagesService: MessagesService,
        private editorDefinitionService: EditorDefinitionService,
        private uriBasedService: UriBasedService,
        private fieldsService: FieldsService,
        private cascadeService: CascadeService
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {        
        if (changes['objectID']) {
            if (!changes['objectID'].isFirstChange() && (changes['objectID'].previousValue != changes['objectID'].currentValue)) { // object has changed            
                this.load();
            }
        }
    }

    private load() {        
        if (this.selection != undefined) {
            this.editedItem = _.cloneDeep(this.selection);
            this.action = this.copy ? "Copy" : this.action;
        }
        else {
            this.editedItem = new Object();
            this.action = this.newActionName;
        }
        this.getDefinition();
    }

    getDefinition() {        
        this.isLoading = true;
        let id = (this.selection ? this.selection[this.rowID] : null);
        this.editorDefinitionService.getEditorDefinition(id, this.objectID, this.objectType, this.parentID, this.targetType, this.targetTypeID, this.createParams, this.editParams, this.action)
            .then(result => {
                this.isLoading = false;
                this.categories = [];

                result = _.orderBy(result, [field => field.Category ? field.Category.toLowerCase() : ''], ['asc']);
                this.fields = result;
                let previousCategory = null;
                let currentCategory = null;
                let rows = [];
                let firstRow = true;

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
                        if (f.Value)
                            f.Value = (f.Value.toUpperCase() == "TRUE" ? true : false); //checkbox doesnt work binding to a string
                        else
                            f.Value = false;
                    }

                    let r = rows.find(r => r.Row == (f.Row || 0));
                    if (r)
                        r.Fields.push(f);
                    else {
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
                if (this.fore != null && this.back != null)
                    this.hasIconFields = true;

                this.form = this.toFormGroup(this.fields);
                this.ref.markForCheck();
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
                }
                else if (field.Value) {
                    name = '';
                    url = field.Value;
                }
                group[field.FieldName + '_Name'] = new FormControl(name || '');
                group[field.FieldName + '_Url'] = new FormControl(url || '', this.getFieldValidators(field));
            }
            else if (field.FieldType == "Date" || field.FieldType == "DateTime") {
                if (field.Value != null)
                    field.Value = new Date(field.Value);
                group[field.FieldName] = new FormControl({ value: (field.Value), disabled: field.ReadOnly }, this.getFieldValidators(field));

            }
            else {
                if (field.FieldType == "Relationship" && this.selection) {
                    if (field.Value != null)
                        field.Value = JSON.parse(field.Value);
                }
                else if (field.FieldType == "Lookup" && !field.Value && this.selection) {
                    let selected = field.Items.filter(x => x.Selected);
                    field.Value = [];
                    for (let item of selected) {
                        field.Value.push(item.Value);
                    }
                    if (field.Value.length == 0) field.Value = null;
                }
                else if (field.FieldType == "Lookup" && field.Value) {
                    if (field.Value != null && field.MultiSelect && typeof field.Value === "string") {
                        field.Value = field.Value.split(',');
                    }
                }
                group[field.FieldName] = new FormControl({ value: (field.Value === null ? '' : field.Value), disabled: field.ReadOnly }, this.getFieldValidators(field));
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
                }
                else if (validation.rule && validation.rule.startsWith('required')) {
                    validators.push(Validators.compose([Validators.required]));
                }
                else if (validation.rule && validation.rule.startsWith('minLength=')) {
                    minLen = +validation.rule.split('=').pop();
                    if (field.FieldType == 'Number' || field.FieldType == 'Decimal') {
                        validators.push(Validators.min(minLen));
                    } else {
                        validators.push(Validators.minLength(minLen));
                    }
                }
                else if (validation.rule && validation.rule.startsWith('maxLength=')) {
                    maxLen = +validation.rule.split('=').pop();
                    if (field.FieldType == 'Number' || field.FieldType == 'Decimal') {
                        validators.push(Validators.max(maxLen));
                    } else {
                        validators.push(Validators.maxLength(maxLen));
                    }

                }
                else if (validation.regex) {
                    validators.push(Validators.pattern(validation.regex));
                }
                validators.push();
            }
        }

        if (field.Required)
            validators.push(Validators.required);

        if (field.FieldType == 'Number') {
            validators.push(FormHelpers.integerValidator);
            if (validators.indexOf(Validators.min) == -1)
                validators.push(Validators.min(minLen));
            if (validators.indexOf(Validators.max) == -1)
                validators.push(Validators.max(maxLen));

        }
        if (field.FieldType == 'Decimal') {
            validators.push(FormHelpers.numberValidator);
            if (validators.indexOf(Validators.min) == -1)
                validators.push(Validators.min(minLen));
            if (validators.indexOf(Validators.max) == -1)
                validators.push(Validators.max(maxLen));
        }
        
        return validators.length > 0 ? validators : null;
    }

    onSubmit() {
        let action = (this.selection == null ? "new" : "edit");
        if (this.copy == true) action = this.action;
        let values: any = {};

        //adjust any dates to utc
        for (var p in this.form.value) {
            if (this.form.value.hasOwnProperty(p)) {
                let field = this.fields.find(f => f.FieldName == p);

                if (this.form.value[p] instanceof Date) {
                    this.form.value[p] = this.getUTCDate(this.form.value[p]);
                }
                else if (field != null && field.FieldType == 'Lookup' && field.UseTypeahead) {
                    if (this.form.value[p] != null)
                        this.form.value[p] = this.form.value[p].Value;
                }
            }
        }

        //takes the form and convert any array values to , separated string values
        for (var p in this.form.value) {
            if (this.form.value.hasOwnProperty(p)) {
                if (Array.isArray(this.form.value[p])) {
                    values[p] = this.form.value[p].join();
                }
                else {
                    values[p] = this.form.value[p];
                }
            }
        }

        if ((this.createUri && action == "new") || (this.editUri && action == "edit")) {
            this.isLoading = true;
            this.uriBasedService.saveItem(this.createUri, this.editUri, values)
                .then(result => {
                    this.showMessageForResult(this.messagesService, result);
                    this.isLoading = false;
                    this.saveClick.emit({ item: result, action: action, values: values });
                });
        } else {
            this.saveClick.emit({ item: values, action: action });
        }
    }

    getUTCDate(date: Date): Date {
        date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
        return date;
    }

    listSelectionChanged(event: any) {
        //look for any fields with this as a parent        
        var field = event.field;
        if (field == null || this.fields == null || this.fields.length <= 0) return;

        var value = event.value;
        if (Array.isArray(event.value)) {
            value = event.value.join();
        }

        this.fields.forEach(editorField => {
            if (editorField.ParentFieldTypeID == field.FieldTypeID) {
                this.cascadeService.cascadeEvent(editorField.FieldTypeID, value);
            }
        });
    }
};
