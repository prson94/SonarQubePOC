///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { EditorDefinitionService, UriBasedService, MessagesService } from '../../services/index';
import { EditorField, EditorRow, FieldValidation } from '../../models/editor-field.model';
import { BaseComponent } from './base.component';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-dynamic-editor',
    template: ` <header>{{action}} {{title}} <div *ngIf="hasCloseButton" (click)="closeClick.emit()" style="cursor: pointer; float: right; font-size: 1.3em"><i class="fa fa-remove"></i></div></header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">                                        
                    <form (ngSubmit)="onSubmit()" [formGroup]="form">                        
                        <div class="row" *ngFor="let row of rows">
                            <div  *ngFor="let field of row.Fields" [class]="'col ' + row.getColClass()" style="padding-bottom:10px;">
                                <d3s-dynamic-field [field]="field" [form]="form"></d3s-dynamic-field>
                            </div>
                        </div>
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!form.valid" style="width: '150px';" label="Save"></button>                            
                            <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: '150px';"></button>
                        </div>                    
                    </form>                    
                </div>
                `,
    providers: [EditorDefinitionService, UriBasedService],
})

export class DynamicEditorComponent extends BaseComponent {
    @Input() selection: any;
    @Input() rowID: string = 'ID';
    @Input() title: string;
    @Input() objectID: number;
    @Input() parentID: number;
    @Input() objectType: string;
    @Input() createUri: string;
    @Input() createParams: any[] = [];
    @Input() editUri: string;
    @Input() editParams: any[] = [];
    @Input() targetType: string;
    @Input() targetTypeID: number;
    @Input() hasCloseButton = false;
    

    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    form: FormGroup;

    action: string = "Edit";
    fields: EditorField[] = [];
    rows: EditorRow[] = [];

    editedItem: any;

    constructor(private messagesService: MessagesService, private editorDefinitionService: EditorDefinitionService, private uriBasedService: UriBasedService) {
        super();
    }

    ngOnInit() {
        if (this.selection != undefined)
            this.editedItem = _.cloneDeep(this.selection);
        else {            
            this.action = "New";
            this.editedItem = new Object();
        }
        this.getDefinition();
        let fb = new FormBuilder();
        this.form = fb.group({
            ContentFee: ['0', Validators.required]
        });
    }

    getDefinition() {      
        this.isLoading = true;  
        let id = (this.selection ? this.selection[this.rowID] : null);
        this.editorDefinitionService.getEditorDefinition(id, this.objectID, this.objectType, this.parentID, this.targetType, this.targetTypeID, this.createParams, this.editParams)
            .then(result => {
                this.isLoading = false;
                this.fields = result;

                this.fields.forEach(f => {
                    if (f.FieldType && f.Value && f.FieldType.toUpperCase() == 'BOOLEAN')
                        f.Value = (f.Value.toUpperCase() == "TRUE" ? true : false); //checkbox doesnt work binding to a string

                    let r = this.rows.find(r => r.Row == (f.Row||0));
                    if (r)
                        r.Fields.push(f);
                    else {
                        let n = new EditorRow();
                        n.Row = f.Row;
                        n.Fields.push(f);
                        this.rows.push(n);
                    }
                });
                                
                this.form = this.toFormGroup(this.fields);
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
                group[field.FieldName + '_Name'] = field.Required ? new FormControl(name || '', Validators.required)
                    : new FormControl(name || '');
                group[field.FieldName + '_Url'] = field.Required ? new FormControl(url || '', Validators.required)
                    : new FormControl(url || '');
            }
            else {                
                group[field.FieldName] = new FormControl(field.Value || '', this.getFieldValidators(field));                  
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
            this.uriBasedService.saveItem(this.createUri, this.editUri, values)
                .then(result => {
                    this.showMessageForResult(this.messagesService, result);                    
                    this.saveClick.emit({ item: result, action: action, values: values});    
                });
        } else {            
            this.saveClick.emit({ item: this.form.value, action: action });     
        }
    }
};