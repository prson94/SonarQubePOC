import { Component } from '@angular/core';
import { Template } from '../../../models/template.model';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { MessagesService } from '../../../services/messages.service';
import { HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import { TemplatesService  } from '../../../services/templates.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-templates',
    template: `                 
                <div class="row">
                <div class="col" [ngClass]="{'s8':isDeleting||isEditing||isAdding}" [ngClass]="{'s12':!isDeleting&&!isEditing&&!isAdding}">                    
                   <div class="tile tile-detail">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <header  *ngIf="!isLoading">Tooltip Templates
                            <d3s-tile-actions [hasAdd]="true" (addClick)="isAdding = true;isEditing=false;isDeleting=false;" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                        </header>    
                        <span  *ngIf="!isLoading">
                            <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                                                  
                            <p-dataTable #dt [globalFilter]="gb" [value]="templates" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selectedTemplate" (onRowDblclick)="isEditing=true;" >                                                        
                                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                <p-column field="Name" header="Name" [sortable]="true" [style]="{width : '150px' }" [filter]="!showSimpleFilter"></p-column>
                                <p-column field="Action" header="Action" [sortable]="true"  [style]="{width : '100px' }" [filter]="!showSimpleFilter"></p-column>                            
                                <p-column header="Description" sortable="true" [filter]="!showSimpleFilter">
                                    <ng-template let-col let-template="rowData" pTemplate type="body">
                                        <div [innerHtml]="template?.Description"></div>
                                    </ng-template>
                                </p-column>                             
                                <p-column [style]="{width:'40px'}">
                                    <ng-template let-template="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a (click)="selectedTemplate=template;isEditing=true;isDeleting=false;isAdding=false;" style="cursor:pointer;"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </ng-template>
                                </p-column>                            
                                <p-column  [style]="{width:'40px'}">
                                    <ng-template let-template="rowData" pTemplate type="body">
                                        <div class="RowTools">                                
                                            <a (click)="selectedTemplate=template;isEditing=false;isDeleting=true;isAdding=false;" style="cursor:pointer;"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </ng-template>
                                </p-column>                            
                            </p-dataTable>
                        </span>
                    </div>
                </div>  
                <div class="col" [ngClass]="{'s4':isDeleting||isEditing||isAdding}">
                    <div class="tile tile-detail" *ngIf="isDeleting">
                    <d3s-delete-form 
                                        [callback]="theDeleteCallback"
                                        [itemId]="selectedTemplate.ID"
                                         [method]="'callback'"
                                         [prompt]="'Are you sure you want to delete ' + [selectedTemplate.Name] + '?'"                                         
                                         (onCancel)="isDeleting=false;"
                                ></d3s-delete-form>
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

    constructor(private templateService: TemplatesService, headerBreadcrumbService: HeaderBreadcrumbService, private messagesService: MessagesService, titleService: Title) {
        super(headerBreadcrumbService, titleService);        
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
        this.isLoading = true;
        this.templateService.deleteTemplateById(id)
            .then(res => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, res);
                //remove the template with this id from the grid
                if (res.type != 'error') {
                    this.templates = this.templates.filter(x => x.ID != id);
                }
                this.isDeleting = false;
            });
    }
    

    addTemplate(event) {
        this.isLoading = true;
        this.templateService.postTemplate(event.template).
            then(res => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, res);
                this.isAdding = false;
                if (res.type != 'error') {
                    event.template.ID = res.id;
                    this.templates[this.templates.length] = event.template;
                }
            });
    }

    updateTemplate(event) {
        this.isLoading = true;
        this.templateService.putTemplate(event.template).
            then(res => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, res);
                var index = this.templates.findIndex(x => x.ID == event.template.ID);
                this.isEditing = false;

                if (index >= 0)
                    this.templates[index] = event.template;
            });
    }    
};
