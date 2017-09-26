import { Component, Input, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { EditorField } from '../../../models/editor-field.model';
import { SelectItem } from 'primeng/primeng';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

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
                        <div *ngSwitchCase="'Relationship'">                           
                            <p-dropdown *ngIf="!field?.MultiSelect" [filter]="true" [options]="field.Items | dropdownItemToSelectItemPipe" [formControlName]="field.FieldName" [(ngModel)]="field.Value" [style]="{width:'100%'}" ngDefaultControl></p-dropdown>
                            <p-multiSelect *ngIf="field?.MultiSelect" [formControlName]="field.FieldName" [(ngModel)]="field.MultipleValues" [options]="field.Items | dropdownItemToSelectItemPipe" [style]="{width:'100%'}" ngDefaultControl></p-multiSelect>
                        </div>
                        <input *ngSwitchCase="'Number'" [formControlName]="field.FieldName" style="width: 100%;" type="number">   
                        <input *ngSwitchCase="'Decimal'" [formControlName]="field.FieldName" style="width: 100%;" type="number" step="any">   
                        <input *ngSwitchCase="'Percentage'" [formControlName]="field.FieldName" style="width: 100%;" type="number" step="0.01" min="0.00" max="1.00">   
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
export class DynamicFieldComponent implements OnInit {
    @Input() field: EditorField;
    @Input() form: FormGroup;

    private regexErrorMessage: string = "The field doesnt meet the required pattern.";
    private fieldTooltip: string;


    private colorValue: string = '#000';

    private isTaxonomyType: boolean = false; // taxonomy type requires its name be mapped to whatever the setting is set to.

    constructor() { }

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

    getLocaleDateString(): string{
        var formats = {
            "ar-SA": "dd/mm/y",
            "bg-BG": "dd.m.yy",
            "ca-ES": "dd/mm/yy",
            "zh-TW": "yy/m/d",
            "cs-CZ": "d.M.yy",
            "da-DK": "dd-mm-yy",
            "de-DE": "dd.mm.yy",
            "el-GR": "d/m/yy",
            "en-US": "m/d/yy",
            "fi-FI": "d.m.yy",
            "fr-FR": "dd/mm/yy",
            "he-IL": "dd/mm/yy",
            "hu-HU": "yy. mm. dd.",
            "is-IS": "d.m.yy",
            "it-IT": "dd/mm/yy",
            "ja-JP": "yy/mm/dd",
            "ko-KR": "yy-mm-dd",
            "nl-NL": "d-m-yy",
            "nb-NO": "dd.mm.yy",
            "pl-PL": "yy-mm-dd",
            "pt-BR": "d/m/yy",
            "ro-RO": "dd.mm.yy",
            "ru-RU": "dd.mm.yy",
            "hr-HR": "d.m.yy",
            "sk-SK": "d. m. yy",
            "sq-AL": "yy-mm-dd",
            "sv-SE": "yy-mm-dd",
            "th-TH": "d/m/yy",
            "tr-TR": "dd.mm.yy",
            "ur-PK": "dd/mm/yy",
            "id-ID": "dd/mm/yy",
            "uk-UA": "dd.mm.yy",
            "be-BY": "dd.mm.yy",
            "sl-SI": "d.m.yy",
            "et-EE": "d.mm.yy",
            "lv-LV": "yy.mm.dd.",
            "lt-LT": "yy.mm.dd",
            "fa-IR": "mm/dd/yy",
            "vi-VN": "dd/mm/yy",
            "hy-AM": "dd.mm.yy",
            "az-Latn-AZ": "dd.mm.yy",
            "eu-ES": "yy/mm/dd",
            "mk-MK": "dd.mm.yy",
            "af-ZA": "yy/mm/dd",
            "ka-GE": "dd.mm.yy",
            "fo-FO": "dd-mm-yy",
            "hi-IN": "dd-mm-yy",
            "ms-MY": "dd/mm/yy",
            "kk-KZ": "dd.mm.yy",
            "ky-KG": "dd.mm.y",
            "sw-KE": "m/d/yy",
            "uz-Latn-UZ": "dd/mm yy",
            "tt-RU": "dd.mm.yy",
            "pa-IN": "dd-mm-y",
            "gu-IN": "dd-mm-y",
            "ta-IN": "dd-mm-yy",
            "te-IN": "dd-mm-y",
            "kn-IN": "dd-mm-y",
            "mr-IN": "dd-mm-yy",
            "sa-IN": "dd-mm-yy",
            "mn-MN": "y.mm.dd",
            "gl-ES": "dd/mm/y",
            "kok-IN": "dd-mm-yy",
            "syr-SY": "dd/mm/yy",
            "dv-MV": "dd/mm/y",
            "ar-IQ": "dd/mm/yy",
            "zh-CN": "yy/m/d",
            "de-CH": "dd.mm.yy",
            "en-GB": "dd/mm/yy",
            "es-MX": "dd/mm/yy",
            "fr-BE": "d/mm/yy",
            "it-CH": "dd.mm.yy",
            "nl-BE": "d/mm/yy",
            "nn-NO": "dd.mm.yy",
            "pt-PT": "dd-mm-yy",
            "sr-Latn-CS": "d.m.yy",
            "sv-FI": "d.m.yy",
            "az-Cyrl-AZ": "dd.mm.yy",
            "ms-BN": "dd/mm/yy",
            "uz-Cyrl-UZ": "dd.mm.yy",
            "ar-EG": "dd/mm/yy",
            "zh-HK": "d/M/yy",
            "de-AT": "dd.mm.yy",
            "en-AU": "d/mm/yy",
            "es-ES": "dd/mm/yy",
            "fr-CA": "yy-mm-dd",
            "sr-Cyrl-CS": "d.m.yy",
            "ar-LY": "dd/mm/yy",
            "zh-SG": "d/M/yy",
            "de-LU": "dd.mm.yy",
            "en-CA": "dd/mm/yy",
            "es-GT": "dd/mm/yy",
            "fr-CH": "dd.mm.yy",
            "ar-DZ": "dd-mm-yy",
            "zh-MO": "d/m/yy",
            "de-LI": "dd.mm.yy",
            "en-NZ": "d/mm/yy",
            "es-CR": "dd/mm/yy",
            "fr-LU": "dd/mm/yy",
            "ar-MA": "dd-mm-yy",
            "en-IE": "dd/mm/yy",
            "es-PA": "mm/dd/yy",
            "fr-MC": "dd/mm/yy",
            "ar-TN": "dd-mm-yy",
            "en-ZA": "yy/mm/dd",
            "es-DO": "dd/mm/yy",
            "ar-OM": "dd/mm/yy",
            "en-JM": "dd/mm/yy",
            "es-VE": "dd/mm/yy",
            "ar-YE": "dd/mm/yy",
            "en-029": "mm/dd/yy",
            "es-CO": "dd/mm/yy",
            "ar-SY": "dd/mm/yy",
            "en-BZ": "dd/mm/yy",
            "es-PE": "dd/mm/yy",
            "ar-JO": "dd/mm/yy",
            "en-TT": "dd/mm/yy",
            "es-AR": "dd/mm/yy",
            "ar-LB": "dd/mm/yy",
            "en-ZW": "m/d/yy",
            "es-EC": "dd/mm/yy",
            "ar-KW": "dd/mm/yy",
            "en-PH": "m/d/yy",
            "es-CL": "dd-mm-yy",
            "ar-AE": "dd/mm/yy",
            "es-UY": "dd/mm/yy",
            "ar-BH": "dd/mm/yy",
            "es-PY": "dd/mm/yy",
            "ar-QA": "dd/mm/yy",
            "es-BO": "dd/mm/yy",
            "es-SV": "dd/mm/yy",
            "es-HN": "dd/mm/yy",
            "es-NI": "dd/mm/yy",
            "es-PR": "dd/mm/yy",
            "am-ET": "d/m/yy",
            "tzm-Latn-DZ": "dd-mm-yy",
            "iu-Latn-CA": "d/mm/yy",
            "sma-NO": "dd.mm.yy",
            "mn-Mong-CN": "yy/m/d",
            "gd-GB": "dd/mm/yy",
            "en-MY": "d/m/yy",
            "prs-AF": "dd/mm/y",
            "bn-BD": "dd-mm-y",
            "wo-SN": "dd/mm/yy",
            "rw-RW": "m/d/yy",
            "qut-GT": "dd/mm/yy",
            "sah-RU": "mm.dd.yy",
            "gsw-FR": "dd/mm/yy",
            "co-FR": "dd/mm/yy",
            "oc-FR": "dd/mm/yy",
            "mi-NZ": "dd/mm/yy",
            "ga-IE": "dd/mm/yy",
            "se-SE": "yy-mm-dd",
            "br-FR": "dd/mm/yy",
            "smn-FI": "d.m.yy",
            "moh-CA": "m/d/yy",
            "arn-CL": "dd-mm-yy",
            "ii-CN": "yy/m/d",
            "dsb-DE": "d. m. yy",
            "ig-NG": "d/m/yy",
            "kl-GL": "dd-mm-yy",
            "lb-LU": "dd/mm/yy",
            "ba-RU": "dd.mm.y",
            "nso-ZA": "yy/mm/dd",
            "quz-BO": "dd/mm/yy",
            "yo-NG": "d/m/yy",
            "ha-Latn-NG": "d/m/yy",
            "fil-PH": "m/d/yy",
            "ps-AF": "dd/mm/y",
            "fy-NL": "d-m-yy",
            "ne-NP": "m/d/yy",
            "se-NO": "dd.mm.yy",
            "iu-Cans-CA": "d/m/yy",
            "sr-Latn-RS": "d.m.yy",
            "si-LK": "yy-mm-dd",
            "sr-Cyrl-RS": "d.m.yy",
            "lo-LA": "dd/mm/yy",
            "km-KH": "yy-mm-dd",
            "cy-GB": "dd/mm/yy",
            "bo-CN": "yy/m/d",
            "sms-FI": "d.m.yy",
            "as-IN": "dd-mm-yy",
            "ml-IN": "dd-mm-y",
            "en-IN": "dd-mm-yy",
            "or-IN": "dd-mm-y",
            "bn-IN": "dd-mm-y",
            "tk-TM": "dd.mm.y",
            "bs-Latn-BA": "d.m.yy",
            "mt-MT": "dd/mm/yy",
            "sr-Cyrl-ME": "d.m.yy",
            "se-FI": "d.m.yy",
            "zu-ZA": "yy/mm/dd",
            "xh-ZA": "yy/mm/dd",
            "tn-ZA": "yy/mm/dd",
            "hsb-DE": "d. m. yy",
            "bs-Cyrl-BA": "d.m.yy",
            "tg-Cyrl-TJ": "dd.mm.y",
            "sr-Latn-BA": "d.m.yy",
            "smj-NO": "dd.mm.yy",
            "rm-CH": "dd/mm/yy",
            "smj-SE": "yy-mm-dd",
            "quz-EC": "dd/mm/yy",
            "quz-PE": "dd/mm/yy",
            "hr-BA": "d.m.yy.",
            "sr-Latn-ME": "d.m.yy",
            "sma-SE": "yy-mm-dd",
            "en-SG": "d/m/yy",
            "ug-CN": "yy-m-d",
            "sr-Cyrl-BA": "d.m.yy",
            "es-US": "m/d/yy"
        };        
        return formats[navigator.language] || 'mm/dd/yy';
    }
}
