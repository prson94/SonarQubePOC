import { Component, Input } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { EditorField } from '../../models/editor-field.model';
import { SelectItem } from 'primeng/primeng';

@Component({
    selector: 'd3s-dynamic-field',
    template: ` <div [formGroup]="form">    
                   <input *ngIf="field.FieldType=='Hidden'" [formControlName]="field.FieldName" [type]="'hidden'" />              
                  <div [ngSwitch]="field.FieldType" class="col s12" *ngIf="field.FieldType!='Hidden'" >
                        <div class="FieldName">{{field.Name}}</div>
                        <input *ngSwitchCase="'Text'" [formControlName]="field.FieldName" style="width: 100%;" [type]="'string'" >                    
                        <p-editor *ngSwitchCase="'Html'" [formControlName]="field.FieldName" [style]="{'height':'150px'}" ngDefaultControl></p-editor>                                                                                                             
                        <div *ngSwitchCase="'Lookup'">
                            <select *ngIf="!field?.MultiSelect" [formControlName]="field.FieldName" style="height:auto;width:100%;">
                                <option *ngFor="let opt of field.Items" [value]="opt.Value">{{opt.Text}}</option>
                            </select>
                            <p-multiSelect *ngIf="field?.MultiSelect" [formControlName]="field.FieldName" [(ngModel)]="field.Value" [options]="field.Items | dropdownItemToSelectItemPipe" [style]="{width:'100%'}" ngDefaultControl></p-multiSelect>
                        </div>
                        <input *ngSwitchCase="'Number'" [formControlName]="field.FieldName" style="width: 100%;" [type]="'number'">   
                        <input *ngSwitchCase="'Color'" [formControlName]="field.FieldName" style="width: 100%;" [type]="'string'">   
                        <input *ngSwitchCase="'Password'" type="password" [formControlName]="field.FieldName" style="width: 100%;" />
                        <input *ngSwitchCase="'Boolean'" type="checkbox" [formControlName]="field.FieldName" />                        
                        <div *ngSwitchCase="'Date'">
                            <p-calendar [formControlName]="field.FieldName"></p-calendar>
                        </div>
                        <div *ngSwitchCase="'Link'">
                            <input [formControlName]="field.FieldName + '_Name'" style="width: 100%;" [type]="'string'" >
                            <div>(Link Name)</div>
                            <input [formControlName]="field.FieldName + '_Url'" style="width: 100%;" [type]="'string'">
                            <div>(Link Url)</div>
                        </div>
                        <div *ngSwitchCase="'FusionLookup'">
                            <select [formControlName]="field.FieldName" style="height:auto;width:100%;">
                                <option *ngFor="let opt of field.Items" [value]="opt.Value">{{opt.Text}}</option>
                            </select>                            
                        </div>
                    <div class="errorMessage" *ngIf="!isValid">*{{field.Name}} is required</div>
                  </div>                   
                </div>
                `,    
})
export class DynamicFieldComponent {
    @Input() field: EditorField;
    @Input() form: FormGroup;
    
    get isValid() { return (this.field.Required && this.field.Value && this.field.Value.length > 0) || !this.field.Required || this.field.FieldType == 'Boolean'; }
    
}
