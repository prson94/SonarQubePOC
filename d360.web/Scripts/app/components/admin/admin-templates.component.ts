import { Component } from '@angular/core';
import { Template } from '../../models/template.model';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService, TemplatesService, PageHeader  } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component'
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-templates',
    template: `                 
                <div class="row">
                <div class="col" [ngClass]="{'s8':isDeleting||isEditing||isAdding}" [ngClass]="{'s12':!isDeleting&&!isEditing&&!isAdding}">                    
                   <div class="tile tile-detail">
                        <div *ngIf="isLoading" style="width:100%; text-align:center;">
                            <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        </div>
                        <header  *ngIf="!isLoading">Tooltip Templates
                            <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Template'" (addClick)="isAdding = true;isEditing=false;isDeleting=false;"></d3s-tile-actions>                            
                        </header>                        
                        <p-dataTable *ngIf="!isLoading" [value]="templates" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selectedTemplate" (onRowDblclick)="isEditing=true;" >                                                        
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
                                        <a (click)="selectedTemplate=template;isEditing=true;isDeleting=false;isAdding=false;" style="cursor:pointer;"><i class="fa fa-pencil"></i></a>                                        
                                    </div>
                                </template>
                            </p-column>                            
                            <p-column  [style]="{width:'40px'}">
                                <template let-template="rowData">
                                    <div class="RowTools">                                
                                        <a (click)="selectedTemplate=template;isEditing=false;isDeleting=true;isAdding=false;" style="cursor:pointer;"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </template>
                            </p-column>                            
                        </p-dataTable>
                    </div>
                </div>  
                <div class="col" [ngClass]="{'s4':isDeleting||isEditing||isAdding}">
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
                    <div class="tile tile-detail" *ngIf="isAdding">
                    <d3s-admin-template-editor                                            
                                            (updateClick)="addTemplate($event)"
                                            (closeClick)="isAdding=false;"
                                        >
                                </d3s-admin-template-editor>
                    </div>
                </div>
               </div>               
                `,
    providers: [TemplatesService],
})

export class AdminTemplatesComponent extends AdminBaseComponent {    
    templates: Template[];
    selectedTemplate: Template; 
    error: any;
    isDeleting: boolean = false;
    isEditing: boolean = false;  
    isAdding: boolean = false;
    
    public theDeleteCallback: Function;

    constructor(pageHeader: PageHeader, private templateService: TemplatesService, headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService);
        this.areaDescription = "All email and tooltip templates for notifications.";
        this.areaName = "Templates";
        this.setCommonItems();
    }

    ngOnInit() {
        this.getTemplates(); 
        this.theDeleteCallback = this.deleteTemplate.bind(this);        
    }
    
    getTemplates() {    
        this.isLoading = true;
        this.templateService
            .getTemplates()
            .then(templates => {
                this.templates = templates;
                this.isLoading = false;
            })
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

    addTemplate(event) {
        this.templateService.postTemplate(event.template);
        this.isAdding = false;
        this.templates[this.templates.length] = event.template;
    }

    updateTemplate(event) {        
        this.templateService.putTemplate(event.template);
        var index = this.findTemplateIndex(event.template.ID);

        if (index >= 0)
            this.templates[index] = event.template;
    }    
};
