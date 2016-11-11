import { Component, Input, OnInit} from '@angular/core';
import { FormGroup } from '@angular/forms';
import { EditorField } from '../../models/editor-field.model';
import { SelectItem } from 'primeng/primeng';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { UriBasedService } from '../../services/index';

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
                        <input *ngSwitchCase="'Text'" [formControlName]="field.FieldName" style="width: 100%;" type="string" (change)="getSimilarItems()" [(ngModel)]="field.Value" >  
                        <div *ngIf="similarItems.length > 0">
                            <div style="color: #FFB230">The following items with similar names already exist:</div>
                            <span *ngFor="let s of similarItems; let i = index;">
                                <d3s-tooltip objectType="Artifact" [objectId]="s.objectid" tooltipType="preview"><a [routerLink]="s.Url">{{s.Name}}</a></d3s-tooltip>
                                <span *ngIf="i < (similarItems.length - 1)">,</span>&nbsp; 
                            </span>
                        </div>                  
                        <p-editor *ngSwitchCase="'Html'" [formControlName]="field.FieldName" [style]="{'height':'150px'}" ngDefaultControl>
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
                        <input *ngSwitchCase="'Number'" [formControlName]="field.FieldName" style="width: 100%;" type="number">   
                        <input *ngSwitchCase="'Decimal'" [formControlName]="field.FieldName" style="width: 100%;" type="number" step="any">   
                        <input *ngSwitchCase="'Percentage'" [formControlName]="field.FieldName" style="width: 100%;" type="number" step="0.01" min="0.01" max="0.99">   
                        <div *ngSwitchCase = "'Color'">
                            <table style="width:100%">
                                <tbody>
                                    <tr>
                                        <td>
                                            <input [(colorPicker)]="colorValue" 
                                                cpOutputFormat="hex"
                                                cpAlphaChannel="disabled"
                                                cpFallbackColor="#000"
                                                cpPosition="bottom"
                                                spellcheck="false"
                                                style="width: 100%;height:25px;" [formControlName]="field.FieldName" [value]="colorValue" (colorPickerChange)="setColorPickerValue($event)"/>
                                        </td>
                                        <td>
                                            <span [style.background-color]="field.Value" style="height:25px;width:25px;display:block;border:1px solid black"></span>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        <input *ngSwitchCase="'Password'" type="password" [formControlName]="field.FieldName" style="width: 100%;" />
                        <input *ngSwitchCase="'Boolean'" type="checkbox" [formControlName]="field.FieldName" />                        
                        <div *ngSwitchCase="'Date'">                            
                            <p-calendar [(ngModel)]="field.Value" [formControlName]="field.FieldName"></p-calendar><span *ngIf="field.Value">{{field.Value|date:'fullDate'}}</span>
                        </div>
                        <div *ngSwitchCase="'DateTime'">                            
                            <p-calendar [(ngModel)]="field.Value" [formControlName]="field.FieldName" [showTime]="true"></p-calendar><span *ngIf="field.Value">{{field.Value|date:'medium'}}</span>
                        </div>
                        <div *ngSwitchCase="'Link'">
                            <input [formControlName]="field.FieldName + '_Name'" style="width: 100%;" type="string" >
                            <div>(Link Name)</div>
                            <input [formControlName]="field.FieldName + '_Url'" style="width: 100%;" type="string">
                            <div>(Link Url)</div>
                        </div>
                        <div *ngSwitchCase="'FusionLookup'">
                            <select [formControlName]="field.FieldName" style="height:auto;width:100%;">
                                <option *ngFor="let opt of field.Items" [value]="opt.Value">{{opt.Text}}</option>
                            </select>                            
                        </div>
                    <div class="errorMessage" *ngIf="!isValid">* {{errorMessage}}</div>
                    
                  </div>                   
                </div>
                `,
    providers: [UriBasedService] 
})
export class DynamicFieldComponent implements OnInit {
    @Input() field: EditorField;
    @Input() form: FormGroup;

    private similarItems = [];
    private regexErrorMessage: string = "The field doesnt meet the required pattern.";
    private fieldTooltip: string;


    private colorValue: string = '#000';

    private isTaxonomyType: boolean = false; // taxonomy type requires its name be mapped to whatever the setting is set to.

    constructor(private uriBasedService: UriBasedService) { }

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
        if (this.form.controls[this.field.FieldName] == undefined) return true;
        if (this.form.controls[this.field.FieldName].disabled) return true;;

        //look at url... fieldname is different.
        if (this.field.FieldType == "Link")
            return this.form.controls[this.field.FieldName + '_Name'].valid && this.form.controls[this.field.FieldName + '_Url'].valid
        else
            return this.form.controls[this.field.FieldName].valid;        
    }

    get errorMessage() {
        if (this.field.FieldType == "Link")
            return this.fieldMessage(this.field.FieldName + '_Name', this.field.Name + ' Name') + ' ' + this.fieldMessage(this.field.FieldName + '_Url', this.field.Name + ' Url');
        else
            return this.fieldMessage(this.field.FieldName, this.field.Name);
    }

    get taxonomyName() {
        return CompanySettings.ArtifactType_TaxonomyTypeID || '';
    }

    get currentFieldName() {
        if (this.isTaxonomyType) return this.taxonomyName;
        return this.field ? this.field.Name : '';
    }

    private fieldMessage(field: string, fieldName: string) {        
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

    getSimilarItems() {
        if (this.field.SimilarItemsUri == null || this.field.SimilarItemsUri == '' || this.field.Value.length < 2)
            return;

        this.similarItems = [];
        this.uriBasedService.getItems(this.field.SimilarItemsUri + this.field.Value)
            .then(r => {
                r.forEach(i => {
                    i.Url = '/' + SiteUrlHelpers.getObjectUrl('Artifact', i.objectid, i.objecttypeid);
                });
                this.similarItems = r;
            });
    }

    setColorPickerValue(e: any) {
        this.form.controls[this.field.FieldName].setValue(e);
        this.field.Value = e;
    }
    
}
