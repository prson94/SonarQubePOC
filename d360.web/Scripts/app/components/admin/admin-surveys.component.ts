import { Component } from '@angular/core';
import {DataTable, Column, Editor, InputText, Dropdown} from 'primeng/primeng';
import {DeleteForm} from '../forms/delete.form';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService, TemplatesService, PageHeader, SurveysService  } from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { AdminBaseComponent } from './admin-base.component'
import { Title } from '@angular/platform-browser';
import { SurveyType } from '../../models/survey.model';

@Component({
    selector: 'd3s-admin-templates',
    template: `                 
                <div class="row">
                    <div class="col s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Surveys
                            <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Survey'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>
                            <div *ngIf="isLoading" style="width:100%; text-align:center;">
                                <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                            </div>
                            <p-dataTable *ngIf="!isLoading && !showDelete && !showEditor" [value]="surveys" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                            <p-column field="Name" header="Name" [sortable]="true" [filter]="true" [style]="{width:'25%'}"></p-column>                                                            
                            <p-column field="Description" header="Description" [sortable]="true" [filter]="true">                            
                                <template let-col let-dimension="rowData">
                                    <div [innerHtml]="dimension?.Description"></div>
                                </template>                                                        
                            </p-column>    
                            <p-column field="ValidForDays" header="Valid Days" [sortable]="true" [filter]="true" [style]="{width:'10%'}"></p-column>
                                <p-column [style]="{width:'40px'}">
                                    <template let-dimension="rowData">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=dimension;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </template>
                                </p-column>                            
                                <p-column  [style]="{width:'40px'}">
                                    <template let-dimension="rowData">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selected=dimension;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </template>
                                </p-column>                            
                            </p-dataTable>                     
                            <delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the survey [' + [selected?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></delete-form>   
                    </div>
                </div>               
               </div>               
                `,
    providers: [SurveysService],
    directives: [DataTable, Column, DeleteForm, Editor, InputText, Dropdown, TileActionsComponent]
})

export class AdminSurveysComponent extends AdminBaseComponent {
    surveys: SurveyType[];
    selected: SurveyType;

    error: any;

    showDelete: boolean = false;
    showEditor: boolean = false;
    

    public theDeleteCallback: Function;

    constructor(pageHeader: PageHeader, private surveysService: SurveysService, headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService);        
        this.areaName = "Surveys";
        this.setCommonItems();
    }

    ngOnInit() {
        this.getTemplates();
        this.theDeleteCallback = this.deleteSurveyType.bind(this);
    }

    getTemplates() {
        this.isLoading = true;
        this.surveysService
            .getSurveyTypes()
            .then(res => {
                this.surveys = res;
                this.isLoading = false;
            })
            .catch(error => this.error = error); // TODO: Display error message
    }

    deleteSurveyType(id: number) {
        //this.surveysService.deleteSurveyTypeById(id);
        //remove the template with this id from the grid
        this.surveys.splice(this.findSurveyTypeIndex(id), 1);
        this.showDelete = false;
    }

    findSurveyTypeIndex(id: number) {
        var index: number = -1;
        for (var survey of this.surveys) {
            index++;
            if (survey.ID == id) return index;
        }
    }

    addSurveyType(event) {
        //this.surveysService.saveSurveyType(event.survey);
        this.showEditor = false;
        this.surveys[this.surveys.length] = event.template;
    }

};
