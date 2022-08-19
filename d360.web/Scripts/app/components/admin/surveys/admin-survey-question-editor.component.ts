import { Input, Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { SurveysService } from '../../../services/surveys.service';
import { QuestionTypeV2 } from '../../../models/survey.model';
import { DropdownOption } from '../../../models/dropdown.model';
import * as _ from 'lodash';
import { NgForm, FormGroup } from '@angular/forms';
import { cloneDeep } from 'lodash';

@Component({
    selector: 'd3s-admin-survey-question-editor',
    template: ` 
               <header>{{action}} <ng-container i18n>Question</ng-container></header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" [hidden]="isLoading">
                    <form (ngSubmit)="onSubmit()" #questionEditorForm="ngForm">
                        <div class="col s6">
                            <div class="FieldName" i18n>Name</div>
                            <div><input required style="width: 100%;" name="name" type="string" [(ngModel)]="editedQuestion.Name" #name="ngModel" maxlength="250"></div>
                            <div [hidden]="name.valid || name.pristine" i18n>Name is required</div>
                        </div>
                        <div class="col s6">
                            <div class="FieldName" i18n>Display Style</div>
                            <div>
                                <select required [(ngModel)]="editedQuestion.DisplayStyle" name="DisplayStyle" #displayStyle="ngModel" style="width:100%;">
                                    <option *ngFor="let p of displayStyles" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>
                            <div [hidden]="displayStyle.valid || displayStyle.pristine" i18n>Display style is required</div>
                        </div>
                        <div class="col l12 s12">
                            <div class="FieldName" i18n>Description</div>
                            <div><p-editor name="Description" [style]="{'height':'150px'}" [ngModel]="editedQuestion?.Description" (ngModelChange)="editedQuestion.Description=$event"></p-editor></div>
                        </div>
                        <div class="row">
                            <span class="FieldName col l11 s11" i18n>Question Options</span>
                            <span class="right-align col l1 s1" (click)="addItem();"><i class="fa fa-plus" aria-hidden="true"></i></span>
                        </div>
                        <div *ngFor="let option of editedQuestion?.Options; let i = index">
                            <div class="row">
                                <div class="col s6">
                                    <input style="width: 100%;margin-bottom:5px;" required [name]="'item_' + i" type="text" [(ngModel)]="option.Name" maxlength="250">
                                    <div *ngIf="questionEditorForm.form.errors && questionEditorForm.form.errors.duplicate_option && questionEditorForm.form.errors.duplicate_option == option.Name" class="error-message">Please enter unique option value</div>
                                </div>
                                <div class="col s6">
                                    <input style="width: 100%;" required [name]="'value_' + i" type="number" [(ngModel)]="option.Value">
                                    <div *ngIf="questionEditorForm.form.errors && questionEditorForm.form.errors.duplicate_identifiers && questionEditorForm.form.errors.duplicate_identifiers.toString() === option.Value.toString()" class="error-message">Please enter unique option identifier</div>
                                </div>
                            </div>
                            <div class="spacer"></div>
                        </div>
                        <div class="col l12 s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton i18n-label type="submit" [disabled]="!questionEditorForm.form.valid" label="Save"></button>
                            <button pButton i18n-label type="button" (click)="closeClick.emit();" label="Close"></button>
                        </div>
                    </form>
                </div> 
                `,
    providers: [SurveysService],
})

export class AdminSurveyQuestionEditorEditor {
    @Input() question: QuestionTypeV2;
    @Input() surveyTypeUid: string;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedQuestion?: QuestionTypeV2 = null;
    isLoading: boolean = false;

    displayStyles: DropdownOption[] = [{ title: $localize`Radio List`, value: "Radio" }, { title: $localize`Check List`, value: "CheckList" }];

    @ViewChild('questionEditorForm', { static: true }) formGroup: NgForm;

    ngOnInit() {
        if (this.question != null) {
            this.editedQuestion = cloneDeep(this.question);
        }
        else {
            this.editedQuestion = {
                Uid: null,
                Description: null,
                DisplayStyle: null,
                Name: null,
                Options: [ { Name: '', Value: 0 } ]
            };

            this.action = "New";
        }

        this.formGroup.form.setValidators(this.duplicatesValidator);
    }

    onSubmit() {
        //save the item back to the save or edit url        
        this.saveClick.emit({ 
            surveyTypeUid: this.surveyTypeUid,
            question: this.editedQuestion, 
            action: this.question != null ? "new" : "edit" 
        });
    }

    addItem() {
        this.editedQuestion.Options.push({ Name: '', Value: 0, });
    }

    private duplicatesValidator(form: FormGroup) {

        function hasDuplicates(array): string {
            var valuesSoFar = Object.create(null);
            for (var i = 0; i < array.length; ++i) {
                var value = array[i];
                if (value in valuesSoFar) {
                    return value;
                }
                valuesSoFar[value] = true;
            }
            return '';
        }

        if (form.value) {
            var keys = Object.keys(form.value);
            var options: string[] = [];
            var option_values: string[] = [];
            keys.forEach((key) => {
                if (key.indexOf('item_') == 0) {
                    options.push(form.value[key]);
                }
                if (key.indexOf('value_') == 0) {
                    option_values.push('no_' + form.value[key]);
                }
            });


            if (hasDuplicates(options)) {
                return { "duplicate_option": hasDuplicates(options) };
            }
            if (hasDuplicates(option_values)) {
                return { "duplicate_identifiers": hasDuplicates(option_values).replace('no_', '') };
            }
        }
        return null;
    }
}