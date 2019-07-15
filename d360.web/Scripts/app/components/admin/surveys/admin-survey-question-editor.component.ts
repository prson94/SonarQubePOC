import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SelectItem } from 'primeng/primeng';
import { SurveysService } from '../../../services/surveys.service';
import { SurveyQuestionType, SurveyQuestionTypeDetails } from '../../../models/survey.model';
import { DropdownOption } from '../../../models/dropdown.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-survey-question-editor',
    template: ` 
                <header>{{action}} Question</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <form (ngSubmit)="onSubmit()" #questionEditorForm="ngForm">                        
                        <div class="col s6">
                            <div class="FieldName">Name</div>
                            <div><input required style="width: 100%;" name="name" type="string" [(ngModel)]="editedQuestion.Name" #name="ngModel" maxlength="250"></div>                            
                            <div [hidden]="name.valid || name.pristine">Name is required</div>
                        </div>                        
                        <div class="col s6">
                            <div class="FieldName">Display Style</div>
                            <div>
                                <select required [(ngModel)]="editedQuestion.DisplayStyle" name="DisplayStyle" #displayStyle="ngModel" style="width:100%;">                                  
                                  <option *ngFor="let p of displayStyles" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>
                            <div [hidden]="displayStyle.valid || displayStyle.pristine">Display style is required</div>
                        </div>                                  
                        <div class="col l12 s12">
                            <div class="FieldName">Description</div>
                            <div><p-editor name="Description" [style]="{'height':'150px'}" [ngModel]="editedQuestion?.Description" (ngModelChange)="editedQuestion.Description=$event" ></p-editor></div>                                                        
                        </div>                        
                        <div class="col l12 s12">
                            <div><span class="FieldName">Question Options</span> <span class="right" (click)="addItem();"><i class="fa fa-plus" aria-hidden="true"></i></span></div>
                            <div>
                                <div class="row" *ngFor="let option of editedQuestion?.Items; let i = index">
                                    <div class="col s6">
                                        <input style="width: 100%;" required [name]="'item_' + i" type="string" [(ngModel)]="option.Name" maxlength="250">
                                    </div>
                                    <div class="col s6">
                                        <input style="width: 100%;" required [name]="'value_' + i" type="number" [(ngModel)]="option.Value">
                                    </div>
                                </div>
                            </div>                                                        
                        </div> 
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!questionEditorForm.form.valid" label="Save"></button>                            
                            <button pButton type="button" (click)="closeClick.emit();" label="Close"></button>
                        </div>                    
                    </form>                           
                </div>
                `,
    providers: [SurveysService],
})

export class AdminSurveyQuestionEditorEditor {
    @Input() questionId: number = 0;
    @Input() surveyTypeId: number = 0;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedQuestion: SurveyQuestionTypeDetails;
    isLoading: boolean = false;

    displayStyles: DropdownOption[] = [{ title: "Radio List", value: "1" }, { title: "Check List", value: "3" }];


    constructor(private surveysService: SurveysService) { }

    ngOnInit() {

        if (this.questionId > 0) {
            this.isLoading = true;
            this.surveysService.getSurveyTypeQuestionDetails(this.questionId, this.surveyTypeId)
                .subscribe(result => {
                    this.editedQuestion = result;
                    this.isLoading = false;
                });
        }
        else {
            this.editedQuestion = new SurveyQuestionTypeDetails();
            this.editedQuestion.SurveyTypeID = this.surveyTypeId;
            this.editedQuestion.Items = [];
            this.editedQuestion.Items.push({
                ID: -1,
                Name: '',
                Value: 0,
                IsChecked: false,
            });
            this.action = "New";
        }
    }

    onSubmit() {
        //save the item back to the save or edit url        
        this.saveClick.emit({ question: this.editedQuestion, action: this.questionId > 0 ? "new" : "edit" });
    }

    addItem() {
        this.editedQuestion.Items.push({ Name: '', Value: 0, ID: 0, IsChecked: false });
    }
};