import { Component, Input } from '@angular/core';
import { FormGroup, REACTIVE_FORM_DIRECTIVES } from '@angular/forms';
import { EditorField } from '../../models/editor-field.model';
import {Button, Editor, InputText, Dropdown, SelectItem, InputMask} from 'primeng/primeng';

@Component({
    selector: 'd3s-dynamic-field',
    template: ` <div [formGroup]="form">    
                   <input *ngIf="field.FieldType=='Hidden'" [(ngModel)]="field.Value"  [formControlName]="field.FieldName" [id]="field.FieldName" [type]="'hidden'" />              
                  <div [ngSwitch]="field.FieldType" class="col s12" *ngIf="field.FieldType!='Hidden'" >
                        <div class="FieldName">{{field.FieldName}}</div>
                        <input *ngSwitchCase="'Text'" [formControlName]="field.FieldName" style="width: 100%;"
                            [id]="field.FieldName" [type]="'string'" [(ngModel)]="field.Value" >                    
                        <p-editor *ngSwitchCase="'Html'" [formControlName]="field.FieldName" [style]="{'height':'150px'}" [(ngModel)]="field.Value" [id]="field.FieldName"></p-editor>                                                                                                             
                        <select [id]="field.FieldName" [formControlName]="field.FieldName" *ngSwitchCase="'Lookup'" [(ngModel)]="field.Value" style="height:auto;">
                            <option *ngFor="let opt of field.Items" [value]="opt.Value">{{opt.Text}}</option>
                        </select>
                        <input *ngSwitchCase="'Number'" [formControlName]="field.FieldName" style="width: 100%;"
                            [id]="field.FieldName" [type]="'string'" [(ngModel)]="field.Value" >   
                        <input *ngSwitchCase="'Color'" [formControlName]="field.FieldName" style="width: 100%;"
                            [id]="field.FieldName" [type]="'string'" [(ngModel)]="field.Value" >   
                    <div class="errorMessage" *ngIf="!isValid">*{{field.Name}} is required</div>
                  </div>                   
                </div>
                `,
    directives: [REACTIVE_FORM_DIRECTIVES, Button, Editor, Dropdown]
})
export class DynamicFieldComponent {
    @Input() field: EditorField;
    @Input() form: FormGroup;
    get isValid() { return (this.field.Required && this.field.Value && this.field.Value.length > 0) || !this.field.Required; }
}
