import { Component, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { SurveysService } from '../../../services/surveys.service';
import { QuestionTypeV2 } from '../../../models/survey.model';
import { DropdownOption } from '../../../models/dropdown.model';
import { cloneDeep } from 'lodash';
import { FormGroup, NgForm } from '@angular/forms';

@Component({
    selector: 'd3s-admin-survey-question-editor',
    templateUrl: './admin-survey-question-editor.component.html',
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
                if (key.indexOf('item_') === 0) {
                    options.push(form.value[key]);
                }
                if (key.indexOf('value_') === 0) {
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