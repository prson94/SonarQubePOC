import { Component } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { TemplatesService } from '../../services/templates.service';
import { HeaderActionsService} from '../../services/header-actions.service';
import { Template } from '../../models/template.model';
import {DataTable, Column, Editor, InputText, Dropdown} from 'primeng/primeng';
import {DeleteForm} from '../forms/delete.form';
import {AdminTemplateEditorComponent} from './admin-template-editor';


@Component({
    selector: 'd3s-admin-templates',
    template: ` 
                <div class="row">
                <div class="col s12">                    
                   <div class="tile tile-detail">
                        <header>Tooltip Templates</header>
                        <p-dataTable [value]="templates" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" >                            
                            <template let-template>                                
                                <delete-form *ngIf="isDeleting"
                                        [callback]="theDeleteCallback"
                                        [itemId]="template.ID"
                                         [method]="'callback'"
                                         [prompt]="'Are you sure you want to delete this template?'"                                         
                                ></delete-form>
                                <d3s-admin-template-editor *ngIf="isEditing" 
                                            [template]="template"
                                            (updateClick)="updateTemplate($event)"
                                            (closeClick)="isEditing=false;"
                                        >
                                </d3s-admin-template-editor>
                            </template>                              
                            <p-column field="Name" header="Name" [sortable]="true" [filter]="true" [style]="{width : '150px' }"></p-column>
                            <p-column field="Action" header="Action" [sortable]="true" [filter]="true" [style]="{width : '100px' }"></p-column>                            
                            <p-column header="Description" sortable="true" filter="true">
                                <template let-col let-template="rowData">
                                    <div [innerHtml]="template?.Description"></div>
                                </template>
                            </p-column>                             
                            <p-column expander="true" [style]="{width:'120px'}">
                                <template let-template="rowData">
                                    <div class="RowTools">
                                        <a (click)="isEditing=true;" style="cursor:pointer;"><i class="fa fa-pencil"></i></a>
                                        <a (click)="showDelete(template)" style="cursor:pointer;"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </template>
                            </p-column>                            
                        </p-dataTable>
                    </div>
                </div>  
               </div>               
                `,
    providers: [TemplatesService],
    directives: [DataTable, Column, DeleteForm, Editor, InputText, Dropdown, AdminTemplateEditorComponent]
})

export class AdminTemplatesComponent {    
    templates: Template[];
    selectedTemplate: Template; 
    error: any;
    isDeleting: boolean = false;
    isEditing: boolean = false;    
    public theDeleteCallback: Function;

    constructor(private pageHeader: PageHeader, private templateService: TemplatesService, private headerActionsService : HeaderActionsService) {
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Templates';
        this.pageHeader.description = 'All email and tooltip templates for notifications.';
        this.templateService = templateService;
        this.headerActionsService = headerActionsService;
    }

    ngOnInit() {
        this.getTemplates(); 
        this.theDeleteCallback = this.deleteTemplate.bind(this);    
    }
    

    getTemplates() {        
        this.templateService
            .getTemplates()
            .then(templates => this.templates = templates)
            .catch(error => this.error = error); // TODO: Display error message
    }

    deleteTemplate(id: number) {     
        this.templateService.deleteTemplateById(id);
        //remove the template with this id from the grid
        this.templates.splice(this.findTemplateIndex(id), 1);
        this.isDeleting = false;
    }

    findTemplateIndex(id: number) {
        var index: number = -1;
        for (var template of this.templates) {            
            index++;
            if (template.ID == id) return index;
        }
    }

    showDelete(template: Template) {
        this.selectedTemplate = template;        
        this.isDeleting = true;        
    }

    updateTemplate(event) {        
        this.templateService.putTemplate(event.template);
        var index = this.findTemplateIndex(event.template.ID);

        if (index >= 0)
            this.templates[index] = event.template;
    }

    addTemplate() {
        //show the add template dialog
    }
};
