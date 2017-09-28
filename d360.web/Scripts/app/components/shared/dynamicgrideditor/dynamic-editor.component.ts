import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { FormArray, FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { EditorDefinitionService } from '../../../services/editor-definition.service';
import { UriBasedService } from '../../../services/uri-based.service';
import { MessagesService } from '../../../services/messages.service';
import { EditorField, EditorRow, FieldValidation, EditorDropDownItem, EditorCategory } from '../../../models/editor-field.model';
import { BaseComponent } from '../base.component';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-dynamic-editor',
    template: ` <header *ngIf="hasHeader">{{action}} {{title}} <div *ngIf="hasCloseButton" (click)="closeClick.emit()" style="cursor: pointer; float: right; font-size: 1.3em"><i class="fa fa-remove"></i></div></header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">                                        
                    <form (ngSubmit)="onSubmit()" [formGroup]="form">        
                        <ng-template ngFor let-category [ngForOf]="categories">
                            <simple-accordion *ngIf="category.name" [header]="category.name" [active]="true">                
                                <div class="row" *ngFor="let row of category.rows">                          
                                    <div *ngFor="let field of row.Fields" [class]="'col ' + row.getColClass()" style="padding-bottom:10px;">                                
                                        <d3s-dynamic-field [field]="field" [form]="form"></d3s-dynamic-field>
                                    </div>
                                </div>
                            </simple-accordion>
                            <span *ngIf="!category.name">
                            <div class="row" *ngFor="let row of category.rows">                          
                                <div *ngFor="let field of row.Fields" [class]="'col ' + row.getColClass()" style="padding-bottom:10px;">                                
                                    <d3s-dynamic-field [field]="field" [form]="form"></d3s-dynamic-field>
                                </div>
                            </div>
                            </span>
                        </ng-template>
                        <div *ngIf="hasIconFields && fore.Value == back.Value" class="col s12 errorMessage">* Foreground and background color cannot be the same</div>
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!form.valid || (hasIconFields && fore.Value == back.Value)" style="width: '150px';" label="Save"></button>                            
                            <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: '150px';"></button>
                        </div>                    
                    </form>                    
                </div>
                `,
    providers: [EditorDefinitionService, UriBasedService],
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

    constructor(private ref: ChangeDetectorRef, private formBuilder: FormBuilder, private messagesService: MessagesService, private editorDefinitionService: EditorDefinitionService, private uriBasedService: UriBasedService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {        
        if (!changes['objectID'].isFirstChange() && (changes['objectID'].previousValue != changes['objectID'].currentValue)) { // object has changed            
            this.load();
        }
    }

    private load() {
        if (this.selection != undefined)
            this.editedItem = _.cloneDeep(this.selection);
        else {
            this.action = this.newActionName;
            this.editedItem = new Object();
        }
        this.getDefinition();       
    }

    getDefinition() {      
        this.isLoading = true;  
        let id = (this.selection ? this.selection[this.rowID] : null);        
        this.editorDefinitionService.getEditorDefinition(id, this.objectID, this.objectType, this.parentID, this.targetType, this.targetTypeID, this.createParams, this.editParams)
            .then(result => {
                this.isLoading = false;
                this.categories = [];
                
                result = _.orderBy(result, [field => field.Category ? field.Category.toLowerCase():''], ['asc']);
                this.fields = result;                
                let previousCategory = null;
                let currentCategory = null;
                let rows = [];                
                let firstRow = true;

                this.fields.forEach(f => {
                    currentCategory = f.Category;
                    if (firstRow) {
                        previousCategory = f.Category;
                        firstRow = false;
                    }

                    if (previousCategory != currentCategory) {
                        let category = new EditorCategory();
                        category.name = previousCategory;
                        category.rows= rows;
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
                //console.log(this.categories);
                                
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
                field.Value = field.Value === null ? '' : new Date(field.Value);
                group[field.FieldName] = new FormControl({ value: (field.Value), disabled: field.ReadOnly }, this.getFieldValidators(field));                
                                
            }
            else {
                if (field.FieldType == "Lookup" && !field.Value && this.selection) {
                    let selected = field.Items.filter(x => x.Selected);                    
                    field.Value = [];
                    for (let item of selected) {                        
                        field.Value.push(item.Value);
                    }
                    if (field.Value.length == 0) field.Value = null;                   
                }
                group[field.FieldName] = new FormControl({ value: (field.Value === null ? '' : field.Value), disabled: field.ReadOnly }, this.getFieldValidators(field));                
            }
        });        
        return new FormGroup(group);
    }

    private getFieldValidators(field: EditorField) {        
        var validators = [];
        
        if (field.Validations) {
            for (let validation of field.Validations) {
                if (validation.rule && validation.rule.startsWith('length=')) {
                    var vals = validation.rule.split(',');
                    if (vals.length == 2) {
                        validators.push(Validators.maxLength(Number(vals[1])));

                        var minParts = vals[0].split('=');
                        if (minParts.length == 2) {
                            let minLen = Number(minParts[1]);
                            if (minLen > 1) {  // only min lenght > 1                                
                                validators.push(Validators.minLength(minLen));
                            }
                        }
                    }
                }
                else if (validation.regex) {
                    validators.push(Validators.pattern(validation.regex));
                }
            }
        }
                       
        if (field.Required)
            validators.push(Validators.required);

        return validators.length > 0 ? validators : null;
    }
    
    onSubmit() {
        let action = (this.selection == null ? "new" : "edit");
        let values: any = {};

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
                    this.saveClick.emit({ item: result, action: action, values: values});    
                });
        } else {            
            this.saveClick.emit({ item: this.form.value, action: action });     
        }
    }
};