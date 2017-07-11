import { Component, OnInit, ChangeDetectionStrategy, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { DiagramService } from '../../services/diagram.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { MessagesService } from '../../services/messages.service';
import { PermissionsService } from '../../services/permissions.service';

@Component({
    selector: 'd3s-mapping-component',
    template: `
        <div class="row">
            <div class="col s12">
                <d3s-loading [isLoading]="isLoading"></d3s-loading>                                 
                <div class="tile tile-detail" *ngIf="!isLoading">                            
                    <header *ngIf="!showDelete && !showEditor">Mappings
                                <d3s-tile-actions [hasAdd]="true" (addClick)="selected=null;showEditor=true;" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                    </header>  
                    <span *ngIf="!showDelete && !showEditor">
                        <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                                                   
                        <p-dataTable #dt sortField="Name" sortOrder="1" [globalFilter]="gb" [value]="mappings" scrollable="true" scrollWidth="100%" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [(selection)]="selected">
                            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                            <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="Transformation" header="Transformation" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="MapClassName" header="Classification" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="MapType" header="Type" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="MapTypeDescription" header="Type Description" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                            <p-column [style]="{width:'28px'}">
                                <ng-template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                    </div>
                                </ng-template>
                            </p-column>                            
                            <p-column  [style]="{width:'28px'}">
                                <ng-template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </ng-template>
                            </p-column>    
                        </p-dataTable>      
                    </span>
                    <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'Map'" [title]="'Map'" [selection]="selected" (saveClick)="saveMap($event)" (closeClick)="showEditor = false;"></d3s-dynamic-editor>
                    <d3s-delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.ID"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the selected item?'"                                         
                        (onCancel)="showDelete=false;"
                    ></d3s-delete-form> 
                </div>
            </div>
        </div>
         `,
    providers: [DiagramService, PermissionsService]
    //changeDetection: ChangeDetectionStrategy.OnPush,
})

export class MappingComponent extends BaseComponent implements OnInit, OnDestroy {
    private mappings: any[] = [];
    private selected: any = null;
    private showEditor: boolean = false;
    private showDelete: boolean = false;
    private theDeleteCallback: Function;

    constructor(protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected diagramService: DiagramService,
        protected messagesService: MessagesService,
        rightSidebarService: RightSidebarService,
        protected permissionsService: PermissionsService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;

        this.setCommonRightSideBar(true, false,false,false,false,true);

        this.theDeleteCallback = this.deleteMapping.bind(this);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Mapping');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Mappings'));

        this.loadPermissions(this.permissionsService, 'Map', 0);

        this.load();
    }

    ngOnDestroy() {
        this.clearSidebar();        
    }

    private load(): void {
        this.isLoading = true;
        this.diagramService.getLineageMappings()
            .then(res => {
                this.isLoading = false;
                for (let item of res) {
                    if (item.MapClass == 1) item.MapClassName = "Source To Target";
                }
                this.mappings = res;
                if (this.selected == null && this.mappings.length > 0) this.selected = this.mappings[0];
            });
    }

    private deleteMapping(id: number): void {
        this.diagramService.deleteLineageMapping(id);        
        this.mappings = this.mappings.filter(x => x.ID != id);
        this.showDelete = false;
    }

    private saveMap(event): void {
        this.diagramService.saveLineageMapping(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.load();                                        
                }
                this.showEditor = false;
            });        
    }
};