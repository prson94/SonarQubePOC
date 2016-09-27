
import { Component, OnDestroy, OnInit} from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, PageHeader, ReportsService, RightSidebarService  } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component';
import { Report, ReportType } from '../../models/report.model';
import { Title } from '@angular/platform-browser';


@Component({
    selector: 'd3s-admin-dashboards-component',
    providers: [ReportsService],
    template: `<d3s-audit *ngIf="isAuditVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'Report'"></d3s-audit>
                <div *ngIf="!isAuditVisible" class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Dashboards
                                <d3s-tile-actions [hasAdd]="true" (addClick)="add()"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showEditor && !showDelete">
                                <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                              
                                <p-dataTable [globalFilter]="gb" [value]="reports" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >                                                                                        
                                    <p-column field="Name" header="Name" [sortable]="true"></p-column>                                                        
                                    <p-column [style]="{width:'40px'}">
                                        <template let-report="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selected=report;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                            </div>
                                        </template>
                                    </p-column>                            
                                    <p-column  [style]="{width:'40px'}">
                                        <template let-report="rowData" pTemplate type="body">
                                            <div class="RowTools">                                
                                                <a style="cursor:pointer;" (click)="selected=report;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </template>
                                    </p-column>    
                                </p-dataTable>  
                            </span>
                            <delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the dashboard [' + [selected?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></delete-form>   
                            <d3s-admin-dashboards-editor *ngIf="!isLoading && showEditor" [report]="selected" (saveClick)="saveReport($event)" (closeClick)="closeEditor()"></d3s-admin-dashboards-editor>                            
                        </div>
                    </div>                                        
                    <div class="col l8 s12" *ngIf="!showEditor && !showDelete">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <object-detail [objectType]="'Report'" [objectID]="selected?.ID"></object-detail>
                                </div>
                            </div>
                        </div>                        
                        <div class="row" *ngIf="isBasicReport(selected)">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-report-item-tile [report]="selected"></d3s-report-item-tile>
                                </div>
                            </div>
                        </div>
                        <div class="row" *ngIf="isBasicReport(selected)">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-report-layout-tile [report]="selected"></d3s-report-layout-tile>
                                </div>
                            </div>
                        </div>
                    <div>
                </div>  
                `
})

export class AdminDashboardsComponent extends AdminBaseComponent implements OnDestroy, OnInit {    
    showEditor: boolean = false;
    showDelete: boolean = false;
    reports: Report[] = [];
    selected: Report;
    theDeleteCallback: Function;

    constructor(rightSidebarService: RightSidebarService, protected reportsService: ReportsService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService, rightSidebarService);
        this.areaDescription = "Manage your dashboard overlays and tiles.";
        this.areaName = "Dashboards";
        this.setCommonItems();
        this.setCommonRightSideBar();
        this.theDeleteCallback = this.deleteReport.bind(this);
    }

    ngOnInit() {
        this.loadReports();      
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private loadReports() {
        this.isLoading = true;
        this.reportsService.getReports().then(result => {
            this.isLoading = false;
            this.reports = result;
            this.selected = (this.reports.length > 0 ? this.reports[0] : null);
        });
    }

    findReportIndex(id: number) {
        var index: number = -1;
        for (var report of this.reports) {
            index++;
            if (report.ID == id) return index;
        }
    }

    deleteReport(id: number) {
        this.reportsService.deleteReport(id);
        this.showDelete = false;
        this.selected = this.reports.length > 0 ? this.reports[0] : null;
        this.reports.splice(this.findReportIndex(id), 1);
    }

    saveReport(event) {
        this.isLoading = true;   
        this.reportsService.saveReport(event.report, event.file)
            .then(result => {
                let parts = event.report.ObjectType.split('|');
                if (parts.length > 0) {
                    event.report.ObjectType = parts[0];
                    event.report.ObjectID = Number(parts[1]);
                }
                if (event.report.ID == undefined) {
                    event.report.ID = Number(result.id);
                    this.reports[this.reports.length] = event.report;
                }
                else {
                    this.reports[this.findReportIndex(event.report.ID)] = event.report;
                }
                
                this.selected = event.report;
                this.isLoading = false;
                this.showEditor = false;
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.reports.length > 0 ? this.reports[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    private isBasicReport(report: Report): boolean {        
        return (report != null && ReportType[report.ReportType] == ReportType.legacy);
    }
}