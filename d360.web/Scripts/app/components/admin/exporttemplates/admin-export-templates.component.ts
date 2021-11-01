import { Component, OnDestroy, OnInit } from '@angular/core';
import { AdminBaseComponent } from '../admin-base.component'
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { ExportTemplateService } from '../../../services/export-template.service';
import { ExportTemplate } from '../../../models/export-template.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-export-templates-component',
    template: ` <div class="row">
                    <div class="col l4 m5 s12">
                        <div class="tile tile-detail">
                            <div>
                    <header>Export Templates
                        <d3s-tile-actions [hasAdd]="!showDelete" (addClick)="selected=null;showEditor=true;showDelete=false;"></d3s-tile-actions>                            
                    </header>                    
                    <span>                        
                        <input type="text" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                        <p-table #dt [value]="exportTemplates" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected" (selectionChange)="selectNode($event)">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'Name'">
                                        Name<d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                    </th>
                                    <th style="width: 28px"></th>
                                    <th style="width: 28px"></th>
                                </tr>
                                <tr [hidden]="showSimpleFilter">
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr [pSelectableRow]="item">
                                    <td>{{item.Name}}</td>
                                    <td>
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="openEditor($event, item)"><i class="fa fa-pencil"></i></a>
                                        </div>
                                    </td>
                                    <td> 
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;showDelete=true;showEditor=false"><i class="fa fa-trash-o"></i></a>
                                        </div>
                                    </td>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>                         
                    </span>                      
                </div>
             </div>
            </div>
            <div class="col l8 m7 s12" *ngIf="showEditor || showDelete">
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                        <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'ExportTemplate'" [title]="'Export Template'" [selection]="selected" (saveClick)="saveExportTemplate($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                        <d3s-delete-form *ngIf="showDelete"
                            [callback]="theDeleteCallback"
                            [itemUid]="selected?.Uid"
                            [method]="'callback'"
                            [prompt]="'Are you sure you want to delete the selected item?'"                                         
                            (onCancel)="showDelete=false;"
                        ></d3s-delete-form>  
                        </div>
                    </div>
                </div>
            </div>
            <div class="col l8 m7 s12" *ngIf="!showEditor && !showDelete">
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail" *ngIf="selected &&!isLoading">  
                            <object-detail [objectType]="'ExportTemplate'" [objectID]="selected?.ID"></object-detail>
                        </div>
                    </div>
                </div>
                <div class="row"><div class="col s12">&nbsp;</div></div>
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail" *ngIf="selected">  
                            <d3s-admin-export-template-fields-component [exportTemplate]="selected" (saveFieldsClick)="saveFields($event)"></d3s-admin-export-template-fields-component>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail" *ngIf="selected">  
                            <header>Template File</header>
                            <div class="row">
                                <div class="col s12">
                                    An Excel template file can be used as the starting point for an export template, providing custom static content on a first worksheet. Exported data will be output to a second worksheet.                                    
                                </div>
                                <div class="col s12">&nbsp;</div>                        
                                <div class="col s2">                                    
                                    <p-fileUpload name="template" [url]="'./api/v2/ExportTemplates/TemplateFile/'+ selected.Uid"
                                        accept=".xls,.xlsx" maxFileSize="10000000" auto="auto"></p-fileUpload>                                                                         
                                </div>     
                                <div class="col s12">
                                    <a style="padding-left:7px;padding-bottom:20px;cursor:pointer;" (click)="clearTemplate()">Remove Template</a>
                                </div>                                
                            </div>
                        </div>
                    </div>
                </div>
                 <div class="row">
                    <div class="col s12">
                        <d3s-admin-export-template-styles [exportViewType]="selected?.ExportViewType" [templateId]="selected?.ID"></d3s-admin-export-template-styles>
                    </div>
                </div>
            </div>
    </div>               
                
                `,
    providers: [ExportTemplateService],       
})

export class AdminExportTemplatesComponent extends AdminBaseComponent implements OnDestroy, OnInit {
    public selected: ExportTemplate;

    exportTemplates: ExportTemplate[];
    showDelete: boolean = false;
    showEditor: boolean = false;
    uploadedFiles: any[] = [];

    theDeleteCallback: Function;
    
    constructor(
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,        
        titleService: Title,
        private exportTemplateService: ExportTemplateService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
        ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_ExportTemplates;
        this.setCommonItems();
                
        this.setCommonSecondaryNavTabs(false);
        this.theDeleteCallback = this.deleteExportTemplate.bind(this);
    }
    
    ngOnDestroy() {
        this.clearSidebar();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.exportTemplateService.getExportTemplates().subscribe(result => {
            this.exportTemplates = result;
            if (this.selected == null && this.exportTemplates != null && this.exportTemplates.length > 0)
                this.selected = this.exportTemplates[0];
            else if (this.selected != null && this.exportTemplates != null && this.exportTemplates.length > 0) {
                let item = this.exportTemplates.filter(x => x.Uid == this.selected.Uid);
                if (item != null && item.length != 0)
                    this.selected = item[0];
            }

            this.getSelectedTemplateID();
            this.isLoading = false;
        });
    }

    public deleteExportTemplate(uid: string) {
            this.exportTemplateService.deleteExportTemplates(uid).subscribe(result => {            
            this.showDelete = false;
            this.selected = null;
            this.load();
        });
    }   

    public saveExportTemplate(event) {
        this.exportTemplateService.saveExportTemplate(event.item).subscribe(result => {            
                this.showEditor = false;
                this.load();                
            });
    }

    public saveFields(event) {
        this.selected.IncludeFieldTypes = event.IncludeFieldTypes;
        this.exportTemplateService.saveExportTemplate(this.selected).subscribe(result => {           
            for (let i = 0; i < this.exportTemplates.length; i++) {
                if (this.exportTemplates[i].Uid == this.selected.Uid) {
                    this.exportTemplates[i].IncludeFieldTypes = this.selected.IncludeFieldTypes;
                    this.getSelectedTemplateID();
                }
            }
        });
    }

    public closeEditor() {
        this.showEditor = false;
    }

    public openEditor(e: any, item: any) {        
        this.showEditor = false;
        this.selected = item;        
        this.selectNode(e, true);
        this.showDelete = false;
    }

    public selectNode(e: any, showEditor: boolean = false) {        
        if (e == null)
            return;
        if (!this.selected.ID) {
            this.getSelectedTemplateID(showEditor);
        } else {
            this.showEditor = showEditor;
        }
        
    }

    public getSelectedTemplateID(showEditor: boolean = false) {
        this.exportTemplateService.getExportTemplateId(this.selected.Uid).subscribe((item) => { this.selected.ID = item; this.showEditor = showEditor;});
    }

    clearTemplate() {
        this.isLoading = true;
        this.exportTemplateService.saveTemplateFile(this.selected).subscribe(result => {
            this.selected.HasTemplateFile = false;
            this.isLoading = false;
        });
    }
}