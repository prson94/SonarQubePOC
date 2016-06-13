import { Component } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { TemplatesService } from '../../services/templates.service';
import { Template } from '../../models/template.model';
import {DataTable, Column, Button, Dialog} from 'primeng/primeng';
import {DeleteForm} from '../forms/delete.form';


@Component({
    selector: 'd3s-admin-templates',
    template: ` <div class="row">
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
                                <div *ngIf="!isDeleting">other stuff</div>
                            </template>                              
                            <p-column field="Name" header="Name" [sortable]="true" [filter]="true" [style]="{width : '150px' }"></p-column>
                            <p-column field="Action" header="Action" [sortable]="true" [filter]="true" [style]="{width : '100px' }"></p-column>
                            <p-column field="Description" header="Description" [sortable]="true" [filter]="true"></p-column>                                                
                            <p-column expander="true" [style]="{width:'120px'}">
                                <template let-template="rowData">
                                    <button pButton type="text" (click)="showDelete(template)" icon="fa fa-trash-o"></button>
                                    <button pButton type="text" (click)="editTemplate(template)" icon="fa fa-pencil"></button>
                                </template>
                            </p-column>                            
                        </p-dataTable>
                    </div>
                </div>  
               </div>               
                `,
    providers: [TemplatesService],
    directives: [DataTable, Column, Button, Dialog, DeleteForm]
})

export class AdminTemplatesComponent {
    pageHeader: PageHeader;
    templateService: TemplatesService;
    templates: Template[];
    selectedTemplate: Template; 
    error: any;
    isDeleting: boolean = false;
    public theDeleteCallback: Function;
    
    constructor(pageHeader: PageHeader, templateService: TemplatesService) {
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Templates';
        this.pageHeader.description = 'All email and tooltip templates for notifications.';
        this.templateService = templateService;        
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

    editTemplate(template: Template) {
        this.templateService.putTemplate(template);
    }
};
