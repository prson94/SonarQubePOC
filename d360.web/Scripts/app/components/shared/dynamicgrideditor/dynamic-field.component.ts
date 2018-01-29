import { Component, Input, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { EditorField } from '../../../models/editor-field.model';
import { SelectItem } from 'primeng/primeng';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { BaseComponent } from '../base.component';
import { FormHelpers } from '../../../static/form-helpers';

declare var CompanySettings;

@Component({
    selector: 'd3s-dynamic-field',
    template: ` <div [formGroup]="form">    
                   <input *ngIf="field.FieldType=='Hidden'" [formControlName]="field.FieldName" type="hidden" />              
                  <div [ngSwitch]="field.FieldType" class="col s12" *ngIf="field.FieldType!='Hidden'" >
                        <div class="FieldName">                            
                            <span *ngIf="fieldTooltip" [pTooltip]="fieldTooltip">{{currentFieldName}}</span>
                            <span *ngIf="!fieldTooltip">{{currentFieldName}}</span>
                        </div>
                        <input *ngSwitchCase="'Text'" [formControlName]="field.FieldName" style="width: 100%;" type="string" [(ngModel)]="field.Value">  
                        <d3s-similar-items *ngIf="field.SimilarItemsUri != null" [uri]="field.SimilarItemsUri" [query]="field.Value"></d3s-similar-items>                                  
                        <p-editor *ngSwitchCase="'Html'" [formControlName]="field.FieldName" [style]="{'height':'150px'}" [(ngModel)]="field.Value">
                            <header style="padding-bottom:0px !important">                                 
                                    <span class="ql-formats">
                                        <select class="ql-header">
                                          <option value="1">Heading</option>
                                          <option value="2">Subheading</option>
                                          <option selected>Normal</option>
                                        </select>
                                        <select class="ql-font">
                                          <option selected>Sans Serif</option>
                                          <option value="serif">Serif</option>
                                          <option value="monospace">Monospace</option>
                                        </select>
                                    </span>
                                    <span class="ql-formats">
                                        <button class="ql-bold"></button>
                                        <button class="ql-italic"></button>
                                        <button class="ql-underline"></button>
                                    </span>
                                    <span class="ql-formats">
                                        <select class="ql-color"></select>
                                        <select class="ql-background"></select>
                                    </span>
                                    <span class="ql-formats">
                                        <button class="ql-list" value="ordered"></button>
                                        <button class="ql-list" value="bullet"></button>
                                        <select class="ql-align">
                                            <option selected></option>
                                            <option value="center"></option>
                                            <option value="right"></option>
                                            <option value="justify"></option>
                                        </select>
                                    </span>
                                    <span class="ql-formats">
                                        <button class="ql-link"></button>                                        
                                        <button class="ql-code-block"></button>
                                    </span>
                                    <span class="ql-formats">
                                        <button class="ql-clean"></button>
                                    </span>                                
                            </header>
                        </p-editor>                                                                                                             
                        <div *ngSwitchCase="'Lookup'">
                            <select *ngIf="!field?.MultiSelect" [formControlName]="field.FieldName" style="height:auto;width:100%;" [(ngModel)]="field.Value">
                                <option></option>
                                <option *ngFor="let opt of field.Items" [value]="opt.Value">{{opt.Text}}</option>
                            </select>
                            <p-multiSelect *ngIf="field?.MultiSelect" [formControlName]="field.FieldName" [(ngModel)]="field.Value" [options]="field.Items | dropdownItemToSelectItemPipe" [style]="{width:'100%'}" ngDefaultControl></p-multiSelect>
                        </div>
                        <div *ngSwitchCase="'Relationship'">                           
                            <p-dropdown *ngIf="!field?.MultiSelect" [filter]="true" [options]="field.Items | dropdownItemToSelectItemPipe" [formControlName]="field.FieldName" [(ngModel)]="field.Value" [style]="{width:'100%'}" ngDefaultControl></p-dropdown>
                            <p-multiSelect *ngIf="field?.MultiSelect" [formControlName]="field.FieldName" [(ngModel)]="field.Value" [options]="field.Items | dropdownItemToSelectItemPipe" [style]="{width:'100%'}" ngDefaultControl></p-multiSelect>
                        </div>
                        <input *ngSwitchCase="'Number'" [formControlName]="field.FieldName" style="width: 100%;" type="number">   
                        <input *ngSwitchCase="'Decimal'" [formControlName]="field.FieldName" style="width: 100%;" type="number" step="any">   
                        <input *ngSwitchCase="'Percentage'" [formControlName]="field.FieldName" style="width: 100%;" type="number" step="0.01" min="0.00" max="1.00" (keyup)="clamp($event, 0, 1, 3)">   
                        <div *ngSwitchCase = "'Color'">
                            <p-colorPicker [(ngModel)]="colorValue" [formControlName]="field.FieldName"></p-colorPicker>                            
                            <input type="text" [(ngModel)]="colorValue" [formControlName]="field.FieldName" style="padding:2px;" />
                        </div>
                        <input *ngSwitchCase="'Password'" type="password" [formControlName]="field.FieldName" style="width: 100%;" />
                        <input *ngSwitchCase="'Boolean'" type="checkbox" [formControlName]="field.FieldName" />                        
                        <div *ngSwitchCase="'Date'">                            
                            <p-calendar [(ngModel)]="field.Value" [formControlName]="field.FieldName" [dateFormat]="getLocaleDateString()"></p-calendar>
                        </div>
                        <div *ngSwitchCase="'DateTime'">                            
                            <p-calendar [(ngModel)]="field.Value" [formControlName]="field.FieldName" [showTime]="true" [dateFormat]="getLocaleDateString()"></p-calendar>
                        </div>
                        <div *ngSwitchCase="'Link'">
                            <input [formControlName]="field.FieldName + '_Name'" style="width: 100%;" type="string" >
                            <div>(Link Name)</div>
                            <input [formControlName]="field.FieldName + '_Url'" style="width: 100%;" type="string">
                            <div>(Link Url: Your Url should start with a protocol prefix.  For example 'http://' or 'https://')</div>
                        </div>
                        <div *ngSwitchCase="'FusionLookup'">
                            <select [formControlName]="field.FieldName" style="height:auto;width:100%;">
                                <option *ngFor="let opt of field.Items" [value]="opt.Value">{{opt.Text}}</option>
                            </select>                            
                        </div>
                        <d3s-multiselect-grid *ngSwitchCase="'DataTableSelect'" [multiple]="field.MultiSelect" [formControlName]="field.FieldName" ngDefaultControl [field]="field" [(ngModel)]="field.Value" ></d3s-multiselect-grid>
                    <div class="errorMessage" *ngIf="!isValid">* {{errorMessage}}</div>
                    
                  </div>                   
                </div>
                `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DynamicFieldComponent extends BaseComponent implements OnInit {
    @Input() field: EditorField;
    @Input() form: FormGroup;

    private regexErrorMessage: string = "The field doesnt meet the required pattern.";
    private fieldTooltip: string;


    private colorValue: string = '#000';

    private isTaxonomyType: boolean = false; // taxonomy type requires its name be mapped to whatever the setting is set to.

    constructor() { super(); }

    ngOnInit() {
        if (this.field && this.field.Validations) {
            for (let validation of this.field.Validations) {
                if (validation.regex) {
                    this.regexErrorMessage = validation.message ? String(validation.message).replace(/<[^>]+>/gm, '') : '';
                }
            }
        }

        if (this.field && this.field.FieldDescription) {
            this.fieldTooltip = this.field.FieldDescription ? String(this.field.FieldDescription).replace(/<[^>]+>/gm, '') : '';
        }

        if (this.field && this.field.FieldName == 'TaxonomyTypeID') {
            this.isTaxonomyType = true;
        }

        if (this.field.FieldType == 'Color') {
            this.colorValue = this.field.Value;

        }
    }

    get isValid() {
        if (this.field.FieldType == "Link") {
            if (this.form.controls[this.field.FieldName + '_Name'] == undefined) return true;
            if (this.form.controls[this.field.FieldName + '_Name'].disabled) return true;

            if (this.form.controls[this.field.FieldName + '_Url'] == undefined) return true;
            if (this.form.controls[this.field.FieldName + '_Url'].disabled) return true;

            return this.form.controls[this.field.FieldName + '_Url'].valid
        }

        if (this.form.controls[this.field.FieldName] == undefined) return true;
        if (this.form.controls[this.field.FieldName].disabled) return true;

        return this.form.controls[this.field.FieldName].valid;
    }

    get errorMessage() {
        if (this.field.FieldType == "Link") {
            return this.fieldMessage(this.field.FieldName + '_Url');
        }
        else
            return this.fieldMessage(this.field.FieldName);
    }

    get taxonomyName() {
        return CompanySettings.ArtifactType_TaxonomyTypeID || '';
    }

    get currentFieldName() {
        if (this.isTaxonomyType) return this.taxonomyName;
        return this.field ? this.field.Name : '';
    }

    private fieldMessage(field: string) {
        if (this.form.controls[field] == undefined) return '';
        var errors = this.form.controls[field].errors;

        if (!errors) return '';
        var message = ""
        if (errors["maxlength"]) {
            message += `${this.currentFieldName} maximum length of ${errors["maxlength"].requiredLength} characters exceeded.  Current length is [${errors["maxlength"].actualLength}]`;
        }

        if (errors["minlength"]) {
            message += `${this.currentFieldName} minimum length of ${errors["minlength"].requiredLength} characters not met.  Current length is [${errors["minlength"].actualLength}]`;
        }

        if (errors["required"]) {
            message += `${this.currentFieldName} is required.  `;
        }

        if (errors["pattern"]) {
            message += this.regexErrorMessage;
        }

        return message;
    }

    setColorPickerValue(e: any) {
        this.form.controls[this.field.FieldName].setValue(e);
        this.field.Value = e;
    }

    private clamp(e: any, min: number, max: number, precision: number) {
        if (e == null || e.target == null || min == null || max == null)
            return;

        let val = e.target.value;

        let newVal = FormHelpers.clamp(val, min, max, precision);

        if (newVal != null && (newVal != 0 || newVal != +val) && !isNaN(newVal)) {
            this.form.controls[this.field.FieldName].setValue(newVal);
            this.field.Value = newVal;
        }
    }    
}
