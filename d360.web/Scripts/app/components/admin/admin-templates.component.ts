import { Component } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { TemplatesService } from '../../services/templates.service';
import { Template } from '../../models/template.model';
import {DataTable, Column, Editor, InputText, Dropdown} from 'primeng/primeng';
import {DeleteForm} from '../forms/delete.form';
import {AdminTemplateEditorComponent} from './admin-template-editor';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';

@Component({
    selector: 'd3s-admin-templates',
    template: `                 
                <div class="row">
                <div class="col s6">                    
                   <div class="tile tile-detail">
                        <header>Tooltip Templates</header>
                        <p-dataTable [value]="templates" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" >                                                        
                            <p-column field="Name" header="Name" [sortable]="true" [filter]="true" [style]="{width : '150px' }"></p-column>
                            <p-column field="Action" header="Action" [sortable]="true" [filter]="true" [style]="{width : '100px' }"></p-column>                            
                            <p-column header="Description" sortable="true" filter="true">
                                <template let-col let-template="rowData">
                                    <div [innerHtml]="template?.Description"></div>
                                </template>
                            </p-column>                             
                            <p-column [style]="{width:'40px'}">
                                <template let-template="rowData">
                                    <div class="RowTools">
                                        <a (click)="selectedTemplate=template;isEditing=true;isDeleting=false;" style="cursor:pointer;"><i class="fa fa-pencil"></i></a>                                        
                                    </div>
                                </template>
                            </p-column>                            
                            <p-column  [style]="{width:'40px'}">
                                <template let-template="rowData">
                                    <div class="RowTools">                                
                                        <a (click)="selectedTemplate=template;isEditing=false;isDeleting=true;" style="cursor:pointer;"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </template>
                            </p-column>                            
                        </p-dataTable>
                    </div>
                </div>  
                <div class="col s6" >
                    <div class="tile tile-detail" *ngIf="isDeleting">
                    <delete-form 
                                        [callback]="theDeleteCallback"
                                        [itemId]="selectedTemplate.ID"
                                         [method]="'callback'"
                                         [prompt]="'Are you sure you want to delete ' + [selectedTemplate.Name] + '?'"                                         
                                         (onCancel)="isDeleting=false;"
                                ></delete-form>
                    </div>
                    <div class="tile tile-detail" *ngIf="isEditing">
                    <d3s-admin-template-editor
                                            [template]="selectedTemplate"
                                            (updateClick)="updateTemplate($event)"
                                            (closeClick)="isEditing=false;"
                                        >
                                </d3s-admin-template-editor>
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

    constructor(private pageHeader: PageHeader, private templateService: TemplatesService, private headerBreadcrumbService: HeaderBreadcrumbService) {
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Templates';
        this.pageHeader.description = 'All email and tooltip templates for notifications.';
        headerBreadcrumbService.clearBreadcrumbs();
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Administration", ""));
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Templates",""));
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
