///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { SurveyQuestionType, SurveyType } from '../../models/survey.model';
import { MessagesService, SurveysService  } from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import {DeleteForm} from '../forms/delete.form';


@Component({
    selector: 'd3s-survey-questions-tile',
    directives: [DataTable, Column, TileActionsComponent, DeleteForm],
    providers: [SurveysService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Questions
                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Question'" (addClick)="add()"></d3s-tile-actions>                            
               </header>
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
               <p-dataTable *ngIf="!isLoading && !showDelete && !showEditor" [value]="questions" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="showEditor=true" [(selection)]="selected" >                                                                        
                    <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                            
                    <p-column field="DisplayStyle" header="Display Type" [sortable]="true" [filter]="true"></p-column>
                    <p-column [style]="{width:'40px'}">
                            <template let-template="rowData">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="showEditor=true"><i class="fa fa-pencil"></i></a>                                      
                                </div>
                            </template>
                    </p-column>                                                
                    <p-column [style]="{width:'40px'}">
                            <template let-template="rowData">
                                <div class="RowTools">                                    
                                    <a style="cursor:pointer;" (click)="showDelete=true"><i class="fa fa-trash-o"></i></a>
                                </div>
                            </template>
                    </p-column>                                                
               </p-dataTable>      
                <delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.Level"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the question [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></delete-form>                 
                `
})

export class SurveyQuestionsTile implements OnChanges {
    @Input() survey: SurveyType = null;
    error: any;
    questions: SurveyQuestionType[] = [];
    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;
    selected: SurveyQuestionType = null;
    theDeleteCallback: Function;

    constructor(private surveysService: SurveysService) {
        this.theDeleteCallback = this.deleteQuestion.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.survey != null) this.getQuestions();
    }

    getQuestions() {
        this.isLoading = true;
        this.surveysService
            .getSurveyTypeQuestions(this.survey)
            .then(res => {
                this.questions = res;
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    deleteQuestion(id: number) {
    //    this.surveysService.deleteTaxonomyLevel(this.survey.ID, id);
        this.showDelete = false;
        this.questions.splice(this.findQuestionById(id), 1);
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.questions.length > 0)
            this.selected = this.questions[0];
    }

    findQuestionById(id: number) {
        var index: number = -1;
        for (var question of this.questions) {
            index++;
            if (question.ID == id) return index;
        }
    }

   /* saveLevel(event) {
        if (event.action == "new") {
            this.taxonomiesService.saveTaxonomyLevel(event.level)
                .then(result => {
                    this.showEditor = false;
                    this.levels[this.levels.length] = event.level;
                    this.selectedLevel = event.level;
                });
        }
        else {
            this.taxonomiesService.editTaxonomyLevel(event.level)
                .then(result => {
                    this.showEditor = false;
                    this.levels[this.findTaxonomyLevel(event.level.Level)] = event.level;
                    this.selectedLevel = event.level;
                });

        }
    }*/
}


