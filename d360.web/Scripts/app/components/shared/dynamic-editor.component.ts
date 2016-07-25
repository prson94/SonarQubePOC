///<reference path="../../../../node_modules/typings/index.d.ts"/>  

import { Input, Component, EventEmitter, Output } from '@angular/core';
import { FormGroup, FormBuilder, Validators, REACTIVE_FORM_DIRECTIVES } from '@angular/forms';
import {Button, Editor, InputText} from 'primeng/primeng';
import { EditorDefinitionService, UriBasedService } from '../../services/index';
import { EditorField, EditorRow } from '../../models/editor-field.model';
import {DynamicFieldComponent} from './dynamic-field.component';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-dynamic-editor',
    template: ` 
                <header>{{action}} {{title}}</header>
                <div class="row">                    
                    <form (ngSubmit)="onSubmit()" [formGroup]="form">
                        
                        <div class="row" *ngFor="let row of rows">
                            <div  *ngFor="let field of row.Fields" [class]="'col ' + row.getColClass()">
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
    directives: [Button, Editor, InputText, REACTIVE_FORM_DIRECTIVES, DynamicFieldComponent]
})

export class DynamicEditorComponent {
    @Input() selection: any;
    @Input() rowID: string = 'ID';
    @Input() title: string;
    @Input() objectID: number;
    @Input() parentID: number;
    @Input() objectType: string;
    @Input() createUri: string;
    @Input() editUri: string;
    

    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    form: FormGroup;

    action: string = "Edit";
    fields: EditorField[] = [];
    rows: EditorRow[] = [];

    editedItem: any;

    constructor(private editorDefinitionService: EditorDefinitionService, private uriBasedService: UriBasedService) { }

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
        let id = (this.selection ? this.selection[this.rowID] : null);
        this.editorDefinitionService.getEditorDefinition(id, this.objectID, this.objectType, this.parentID)
            .then(result => {
                this.fields = result;

                this.fields.forEach(f => {
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
                
                console.log(this.fields);
                this.form = this.editorDefinitionService.toFormGroup(this.fields);
            });
    }   
    
    onSubmit() {
        let action = (this.selection == null ? "new" : "edit");
        if ((this.createUri && action == "new") || (this.editUri && action == "edit")) {
            this.uriBasedService.saveItem(this.createUri, this.editUri, this.form.value)
                .then(result => {
                    this.saveClick.emit({ item: result, action: action});    
                });
        } else {
            //console.log(JSON.stringify(this.form.value));
            this.saveClick.emit({ item: this.form.value, action: action });     
        }
    }
};