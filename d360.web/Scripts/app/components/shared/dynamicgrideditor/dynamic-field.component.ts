import { Component, Input, OnInit, ChangeDetectionStrategy, Output, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { EditorField } from '../../../models/editor-field.model';
import { SelectItem } from 'primeng/primeng';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { BaseComponent } from '../base.component';
import { FormHelpers } from '../../../static/form-helpers';
import { CascadeService } from '../../../services/cascade.service';
import { FieldsService } from '../../../services/fields.service';
import { concat } from 'rxjs/observable/concat';
import { Subject } from 'rxjs';

declare var CompanySettings;

@Component({
    selector: 'd3s-dynamic-field',
    template: ` <div [formGroup]="form">    
                   <input *ngIf="field.FieldType=='Hidden'" [formControlName]="field.FieldName" type="hidden" />              
                  <div [ngSwitch]="field.FieldType" class="col s12" *ngIf="field.FieldType!='Hidden'" >
                        <div class="FieldName">                            
                            <span *ngIf="fieldTooltip" [pTooltip]="fieldTooltip">{{currentFieldName}}</span>
                            <span *ngIf="!fieldTooltip">{{currentFieldName}}&nbsp;</span>
                        </div>
                        <input *ngSwitchCase="'Text'" [formControlName]="field.FieldName" (blur)="OnBlurTrim()" style="width: 100%;" type="string" [(ngModel)]="field.Value">  
                        <d3s-similar-items *ngIf="field.SimilarItemsUri != null" [uri]="field.SimilarItemsUri" [query]="field.Value"></d3s-similar-items>                                  
                        <p-editor *ngSwitchCase="'Html'" [formControlName]="field.FieldName"  [style]="{'height':'150px'}" [(ngModel)]="field.Value">
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
                            <p-multiSelect *ngIf="field?.MultiSelect;else singleSelectList" [formControlName]="field.FieldName" [ngModel]="field.Value" (ngModelChange)="listItemChange.emit({field:field,value:$event});field.Value=$event;" [options]="field.Items | dropdownItemToSelectItemPipe" [style]="{width:'100%'}" ngDefaultControl [defaultLabel]="multiselectLabel()"></p-multiSelect>
                            <ng-template #singleSelectList>                                
                                <select [formControlName]="field.FieldName" style="height:auto;width:100%;" [ngModel]="field.Value" (ngModelChange)="listItemChange.emit({field:field,value:$event});field.Value=$event;">
                                    <option *ngIf="field.ParentFieldTypeName && (!field.Items || field.Items.length == 0);else blankOption" value="" disabled selected>Select a {{field.ParentFieldTypeName}}</option>
                                    <ng-template #blankOption>
                                        <option value=""></option>
                                    </ng-template>
                                    <option *ngFor="let opt of field.Items" [value]="opt.Value">{{opt.Text}}</option>
                                </select>
                            </ng-template>                            
                        </div>
                        <div *ngSwitchCase="'Relationship'">
                            <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                            <p-dataTable #dt
                                            [globalFilter]="gb"
                                            [loading]="relationItemsLoading"
                                            loadingIcon="fa-spinner"
                                            scrollable="true"
                                            scrollWidth="100%"
                                            [rowsPerPageOptions]="defaultPagingOptions"
                                            [value]="field.Items"
                                            [selection]="relationItems"
                                            (selectionChange)="selectRelationItems($event)"
                                            [formControlName]="field.FieldName"
                                            [rows]="defaultInitialItemsPerPage"
                                            paginator="true"
                                            pageLinks="3"
                                            lazy="true"
                                            (onLazyLoad)="lazyLoad($event)"
                                            [totalRecords]="field?.RecordCount"
                                            ngDefaultControl>
                                    <p-footer *ngIf="dt.totalRecords">
                                        <d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info>
                                    </p-footer>
                                    <p-column field="Selected" [selectionMode]="field?.MultiSelect ? 'multiple' : 'single'" [style]="{'width':'30px'}"></p-column>
                                    <p-column field="Text" header="Name"></p-column>
                                </p-dataTable>
                        </div>

                        <input *ngSwitchCase="'Number'" [(ngModel)]="field.Value" [formControlName]="field.FieldName" style="width: 100%;" type="string">              
                        <input *ngSwitchCase="'Decimal'" [(ngModel)]="field.Value" [formControlName]="field.FieldName" style="width: 100%;" type="string">   

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
    providers: [FieldsService]
})
export class DynamicFieldComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() field: EditorField;
    @Input() form: FormGroup;
    @Input() object: string;
    @Input() objectID: number = null;
    
    @Output() listItemChange = new EventEmitter();

    private regexErrorMessage: string = "The field doesnt meet the required pattern.";
    private fieldTooltip: string;
    private cascadeSub: any;
    private relationSource$ = new Subject<any>();
    private relationSub: any;
    private relationItems = [];
    private relationItemsLoading = false;

    private colorValue: string = '#000';

    private isTaxonomyType: boolean = false; // taxonomy type requires its name be mapped to whatever the setting is set to.
    private hasCascadeLoaded: boolean = false;

    constructor(
        private cascadeService: CascadeService,
        private fieldsService: FieldsService,
        private ref: ChangeDetectorRef
    ) {
        super();
    }

    ngOnInit() {
        this.cascadeSub = this.cascadeService.cascadeMessage$.subscribe(
            casc => {
                if (this.field.ParentFieldTypeID > 0 && casc.fieldTypeId == this.field.FieldTypeID) {
                    if (casc.parentListItemId != null && casc.parentListItemId.length > 0) {
                        //load the values for the list that is a child                    
                        this.field.Items = [];                        
                        return this.fieldsService.getCascadingListFieldValues(casc.fieldTypeId, casc.parentListItemId).then(res => {
                            
                            this.field.Items = res;
                            if (((this.field.Items == null || this.field.Items.length == 0) && this.field.Value != null) || this.hasCascadeLoaded) {
                                this.field.Value = null;                                
                            }                            
                            this.hasCascadeLoaded = true;
                            this.listItemChange.emit({ field: this.field, value: this.field.Value });                                
                            this.ref.markForCheck();
                        })
                    }
                    else {
                        this.field.Value = null;
                        this.listItemChange.emit({ field: this.field, value: null });    
                    }
                }
            });

        this.relationSub = this.fieldsService.getRelationshipFieldItems(this.relationSource$)
            .subscribe(res => {
                this.relationItemsLoading = false;
                this.field.Items = res.results.items;
                this.selectRelationItems(this.relationItems);

                if ((res.event.globalFilter != null && res.event.globalFilter != "") || res.event.first == 0)
                    this.field.RecordCount = res.results.count;
                this.ref.markForCheck();
        });


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

        if (this.field.FieldType == 'Relationship') {
            this.selectRelationItems(this.field.Value);
        }

        if (this.field.FieldType == 'Lookup' && this.field.ParentFieldTypeID <= 0) {
            window.setTimeout(() => {                
                this.listItemChange.emit({ field: this.field, value: this.field.Value });
            }, 250);            
        }
    }

    ngOnDestroy() {
        this.cascadeSub.unsubscribe();
        this.relationSub.unsubscribe();
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

        if (errors["pattern"]) {
            message += this.regexErrorMessage;
        }
        if (errors["number"]) {
            message += "Please enter a valid number";
        }
        if (errors["integer"]) {
            message += "Please enter a valid integer";
        }
        if (errors["maxlength"]) {
            message += `${this.currentFieldName} maximum length of ${errors["maxlength"].requiredLength} characters exceeded.  Current length is [${errors["maxlength"].actualLength}]`;
        }

        if (errors["minlength"]) {
            message += `${this.currentFieldName} minimum length of ${errors["minlength"].requiredLength} characters not met.  Current length is [${errors["minlength"].actualLength}]`;
        }

        if (errors["required"]) {
            message += `${this.currentFieldName} is required.  `;
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

    multiselectLabel(): string {
        if (this.field && this.field.ParentFieldTypeName && this.field.ParentFieldTypeName.length > 0 && (this.field.Items == null || this.field.Items.length == 0))
            return `Select a ${this.field.ParentFieldTypeName}`;
        return "Choose";
    }

    OnBlurTrim() {
        let value: string = this.form.controls[this.field.FieldName].value;
        this.form.controls[this.field.FieldName].setValue(value.trim());
    }

    private lazyLoad(e: any) {
        //console.log('lazyLoad', this.relationItems, { fieldTypeID: this.field.FieldTypeID, object: this.object, objectID: this.objectID, event: e });
        this.relationItemsLoading = true;
        this.relationSource$.next({ fieldTypeID: this.field.FieldTypeID, object: this.object, objectID: this.objectID, event: e });
    }

    selectRelationItems(e: any) {
        if (e === '[]')
            this.relationItems = [];
        else
            this.relationItems = e;

        if (this.relationItems != null) {
            if (!Array.isArray(this.relationItems)) 
                this.relationItems = [this.relationItems];

            for (let i = 0; i < this.relationItems.length; i++) { //associate the selection with the item in the table
                let x = this.field.Items.findIndex(f => f.Value == this.relationItems[i].Value);
                if (x > -1) {
                    this.relationItems[i] = this.field.Items[x];
                }
            } 
            
            this.relationItems = this.relationItems.slice();
            this.field.Value = this.relationItems.map(i => i.Value).join(',');
        } else {
            this.field.Value = null;
        }

        this.form.controls[this.field.FieldName].setValue(this.field.Value);
        this.ref.markForCheck();

        //console.log(e, this.relationItems, this.field);
    }

}
