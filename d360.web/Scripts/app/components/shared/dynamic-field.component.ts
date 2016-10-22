import { Component, Input} from '@angular/core';
import { FormGroup } from '@angular/forms';
import { EditorField } from '../../models/editor-field.model';
import { SelectItem } from 'primeng/primeng';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { UriBasedService } from '../../services/index';

@Component({
    selector: 'd3s-dynamic-field',
    template: ` <div [formGroup]="form">    
                   <input *ngIf="field.FieldType=='Hidden'" [formControlName]="field.FieldName" type="hidden" />              
                  <div [ngSwitch]="field.FieldType" class="col s12" *ngIf="field.FieldType!='Hidden'" >
                        <div class="FieldName">{{field.Name}}</div>
                        <input *ngSwitchCase="'Text'" [formControlName]="field.FieldName" style="width: 100%;" type="string" (change)="getSimilarItems()" [(ngModel)]="field.Value" >  
                        <div *ngIf="similarItems.length > 0">
                            <div style="color: #FFB230">The following items with similar names already exist:</div>
                            <span *ngFor="let s of similarItems; let i = index;">
                                <d3s-tooltip objectType="Artifact" [objectId]="s.objectid" tooltipType="preview"><a [routerLink]="s.Url">{{s.Name}}</a></d3s-tooltip>
                                <span *ngIf="i < (similarItems.length - 1)">,</span>&nbsp; 
                            </span>
                        </div>                  
                        <p-editor *ngSwitchCase="'Html'" [formControlName]="field.FieldName" [style]="{'height':'150px'}" ngDefaultControl></p-editor>                                                                                                             
                        <div *ngSwitchCase="'Lookup'">
                            <select *ngIf="!field?.MultiSelect" [formControlName]="field.FieldName" style="height:auto;width:100%;" [(ngModel)]="field.Value">
                                <option *ngFor="let opt of field.Items" [value]="opt.Value">{{opt.Text}}</option>
                            </select>
                            <p-multiSelect *ngIf="field?.MultiSelect" [formControlName]="field.FieldName" [(ngModel)]="field.Value" [options]="field.Items | dropdownItemToSelectItemPipe" [style]="{width:'100%'}" ngDefaultControl></p-multiSelect>
                        </div>
                        <input *ngSwitchCase="'Number'" [formControlName]="field.FieldName" style="width: 100%;" type="number">   
                        <input *ngSwitchCase="'Color'" [formControlName]="field.FieldName" style="width: 100%;" type="string">   
                        <input *ngSwitchCase="'Password'" type="password" [formControlName]="field.FieldName" style="width: 100%;" />
                        <input *ngSwitchCase="'Boolean'" type="checkbox" [formControlName]="field.FieldName" />                        
                        <div *ngSwitchCase="'Date'">
                            <p-calendar [formControlName]="field.FieldName"></p-calendar>
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
export class DynamicFieldComponent {
    @Input() field: EditorField;
    @Input() form: FormGroup;

    private similarItems = [];

    constructor(private uriBasedService: UriBasedService) { }
        

    get isValid() {        
        if (this.form.controls[this.field.FieldName] == undefined) return true;
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

    private fieldMessage(field: string, fieldName: string) {        
        if (this.form.controls[field] == undefined) return '';
        var errors = this.form.controls[field].errors;
        var message = ""
        if (errors["maxlength"]) {
            message += `${this.field.Name} maximum length of ${errors["maxlength"].requiredLength} characters exceeded.  Current length is [${errors["maxlength"].actualLength}]`;
        }

        if (errors["minlength"]) {
            message += `${this.field.Name} minimum length of ${errors["minlength"].requiredLength} characters not met.  Current length is [${errors["minlength"].actualLength}]`;
        }

        if (errors["required"]) {
            message += `${this.field.Name} is required.  `;
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
    
}
