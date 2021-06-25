import { Component, OnDestroy, OnInit} from '@angular/core';
import { NgForm } from '@angular/forms';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { ReportsService } from '../../../services/reports.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Report } from '../../../models/report.model';
import { Title } from '@angular/platform-browser';
import { StateService } from '../../../services/state.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';


@Component({
    selector: 'd3s-admin-dashboards-component',
    providers: [ReportsService],
    template: `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Dashboards
                                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" [hasAuthenticate]="true" (authenticateClick)="showCredentials=true;powerBiUser='';powerBiPassword=''"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showEditor && !showDelete && !showCredentials">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="reports" selectionMode="single" [globalFilterFields]="['Name','DisplayType']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="20" [(selection)]="selected" (onRowSelect)="selectedItemChange()">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'Name'">
                                                Name
                                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'DisplayType'">
                                                Type
                                                <d3s-sortIcon [field]="'DisplayType'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 40px"></th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'DisplayType'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selected=item;showEditor=true;" [pSelectableRow]="item">
                                            <td>{{item.Name}}</td>
                                            <td>{{item.DisplayType}}</td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table>
                            </span>
                            <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the dashboard [' + [selected?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></d3s-delete-form>   
                            <d3s-admin-dashboards-editor *ngIf="!isLoading && showEditor" [report]="selected" (saveClick)="saveReport($event)" (closeClick)="closeEditor()"></d3s-admin-dashboards-editor>                            
                            <span *ngIf="showCredentials">
                                <form (ngSubmit)="onSubmitPowerCreds()" #powerBICredsForm="ngForm">
                                    <div class="form-instructions">Specify the credentials to be used for Power BI Direct Query type queries.</div>
                                    <div class="row">                                
                                        <div class="col s12">
                                            <div class="FieldName">Username:</div>
                                            <div><input name="user" required type="text" [(ngModel)]="powerBiUser" #name="ngModel" style="width:100%"></div>
                                            <div [hidden]="name.valid || name.pristine">Name is required</div>
                                        </div>
                                        <div class="col s12">
                                            <div class="FieldName">Password:</div>
                                            <div><input required name="pwd" type="password" [(ngModel)]="powerBiPassword" #pwd="ngModel" style="width:100%"></div>
                                            <div [hidden]="pwd.valid || pwd.pristine">Password is required</div>
                                        </div>
                                        <div class="col s12">&nbsp;</div>
                                        <div class="col s12">
                                            <button pButton type="submit" [disabled]="!powerBICredsForm.form.valid" label="Save"></button>                            
                                            <button pButton type="button" (click)="showCredentials=false;" label="Close"></button>
                                        </div>  
                                    </div>
                                </form>
                            </span>
                        </div>
                    </div>                                        
                    <div class="col l8 s12" *ngIf="!showEditor && !showDelete && selected">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <object-detail [objectType]="'Report'" [objectID]="selected?.ID"></object-detail>
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
    showCredentials: boolean = false;
    reports: Report[] = [];
    selected: Report;
    theDeleteCallback: Function;
    powerBiUser: string;
    powerBiPassword: string;

    constructor(private stateService: StateService, secondaryNavService: SecondaryNavService, protected reportsService: ReportsService, protected messagesService: MessagesObservableService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, secondaryNavService);        
        this.areaName = StringConstants.Section_Dashboards;
        this.theDeleteCallback = this.deleteReport.bind(this);
    }
    
    selectedItemChange() {
        if (this.selected)
            this.buildSecondaryNavigationForObject(this.selected.ID, 'Report');
    }

    ngOnInit() {
        this.loadReports();      
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private loadReports() {
        this.isLoading = true;
        this.reportsService.getReports().subscribe(result => {
            this.isLoading = false;
            for (var report of result) {
                if (report.ReportType == 'sagacity') report.DisplayType = 'Data360 DQ+';
                else report.DisplayType = report.ReportType;
            }
            this.reports = result;            
            this.selected = (this.reports.length > 0 ? this.reports[0] : null);
            this.selectedItemChange();
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
        this.reportsService.deleteReport(id)
            .subscribe(result => {
                this.showDelete = false;
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.selected = this.reports.length > 0 ? this.reports[0] : null;
                    this.reports.splice(this.findReportIndex(id), 1);
                    this.selectedItemChange();
                }

                this.stateService.reloadLeftNavMenu();
            });
    }

    saveReport(event) {
        this.isLoading = true;   
        this.reportsService.saveReport(event.report, event.file)
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
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

                if (event.report.ReportType == 'sagacity') event.report.DisplayType = 'Data360 DQ+';
                else event.report.DisplayType = event.report.ReportType;
                
                if (result.type == "error") {
                    this.showEditor = true;
                } else {
                    this.showEditor = false;
                }
                this.isLoading = false;
                this.selected = event.report;
                this.selectedItemChange();

                this.stateService.reloadLeftNavMenu();
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.reports.length > 0 ? this.reports[0] : null;
            this.selectedItemChange();
        }
    }

    add() {
        this.showEditor = true;
        this.selected = null;
        this.selectedItemChange();
    }

    private onSubmitPowerCreds() {
        this.isLoading = true;
        this.reportsService.setPowerBICredentials(this.powerBiUser, this.powerBiPassword)
            .subscribe(result => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.showCredentials = false;                    
                }
            });        
    }
}