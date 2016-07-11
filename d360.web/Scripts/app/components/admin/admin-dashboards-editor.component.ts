///<reference path="../../../../node_modules/typings/index.d.ts"/>  

import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm, REACTIVE_FORM_DIRECTIVES } from '@angular/forms';
import {Button, Editor, InputText, Dropdown, SelectItem} from 'primeng/primeng';
import { ReportsService} from '../../services/index';
import { Report, ReportType} from '../../models/report.model';
import { DropdownOption } from '../../models/dropdown.model';

import _ from 'lodash';

@Component({
    selector: 'd3s-admin-dashboards-editor',
    template: ` 
                <header>{{action}} Report</header>
                <div *ngIf="isLoading()">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div class="row" *ngIf="!isLoading()">
                    <div class="form-instructions">Add a report to the list of reports, which can then be exposed in other areas of this system.</div>            
                    <form (ngSubmit)="onSubmit()" #reportForm="ngForm">                        
                        <div class="col s12">
                            <div class="FieldName">Report Type</div>
                            <div>                                
                                <select required [(ngModel)]="editedReport.ReportType" name="reportType" #reportType="ngModel" style="width:100%;">
                                  <option *ngFor="let p of reportTypes" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>       
                            <div [hidden]="reportType.valid || reportType.pristine">A report type is required</div>                     
                        </div>                        
                        <div class="col s12">
                            <div class="FieldName">Name</div>
                            <div><input required style="width: 100%;" name="name" [type]="'string'" [(ngModel)]="editedReport.Name" #name="ngModel"></div>     
                            <div [hidden]="name.valid || name.pristine">A name is required</div>                                                   
                        </div>     
                        <div class="col s12">
                            <div class="FieldName">Target Type</div>
                            <div>                                
                                <select required [(ngModel)]="editedReport.ObjectType" name="targetType" #targetType="ngModel" style="width:100%;">
                                  <option *ngFor="let p of targetTypes" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>       
                            <div [hidden]="targetType.valid || targetType.pristine">A target type is required</div>                                                                        
                        </div>
                        <div class="col s12" *ngIf="editedReport.ReportType != 'powerbi'">
                            <div class="FieldName">Report Layout</div>
                            <div>                                
                                <select required [(ngModel)]="editedReport.ReportLayoutID" name="ReportLayout" #reportLayout="ngModel" style="width:100%;">
                                  <option *ngFor="let p of reportLayouts" [value]="p.value">{{p.title}}</option>
                                </select>                                
                            </div>       
                            <div [hidden]="reportLayout.valid || reportLayout.pristine">A report layout is required</div>                                                                        
                        </div>
                        <div class="col s12" *ngIf="editedReport.ReportType == 'powerbi'">
                            <div class="FieldName">File</div>
                            <div><input type="file" (change)="changeFile($event);" accept=".pbix" style="width: 99%" name="File" formenctype="multipart/form-data" /></div>
                        </div>
                        <div class="col s12">
                            <div class="FieldName">Description</div>
                            <p-editor name="description" [style]="{'height':'150px'}" [(ngModel)]="editedReport.Description"></p-editor>
                        </div>                    
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!reportForm.form.valid" style="width: '150px';" label="Save"></button>                            
                            <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: '150px';"></button>
                        </div>                    
                    </form>                           
                </div>
                `,
    providers: [ReportsService],
    directives: [Button, Editor, InputText, Dropdown, REACTIVE_FORM_DIRECTIVES]
})

export class AdminDashboardsEditor {
    @Input() report: Report;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedReport: Report;
    isLayoutsLoading: boolean = false;
    isTargetsLoading: boolean = false;
    reportTypes: DropdownOption[] = [];
    targetTypes: DropdownOption[] = [];
    reportLayouts: DropdownOption[] = [];
    file: File;
    
    constructor(private reportsService: ReportsService) {        
        this.reportTypes.push({ value:"legacy", title:"Default" });
        this.reportTypes.push({ value:"powerbi", title:"PowerBI" });
    }

    ngOnInit() {
        if (this.report != undefined) {
            this.editedReport = _.cloneDeep(this.report);
            this.editedReport.ObjectType = this.editedReport.ObjectType + '|' + this.editedReport.ObjectID.toString();
        }
        else {
            this.editedReport = new Report();
            this.action = "New";
        }   
        this.getReportTargets();
        this.getReportLayouts();
    }

    onSubmit() {
        /*if (this.editedReport.ReportType == 'powerbi') {
            this.getFile(this.file).then(result => {
                this.saveClick.emit({ report: this.editedReport, action: this.report ? "new" : "edit" });
            });
        }*/
        this.saveClick.emit({ report: this.editedReport, action: this.report ? "new" : "edit", file: this.file });
    }

    getReportTargets() {
        this.isTargetsLoading = true;
        this.reportsService.getReportTargetTypes()
            .then(result => {
                this.targetTypes = result;                
                this.isTargetsLoading = false;
            });
    }

    getReportLayouts() {
        this.isLayoutsLoading = true;
        this.reportsService.getReportLayouts()
            .then(result => {
                this.reportLayouts = result;
                this.isLayoutsLoading = false;
            });
    }

    isLoading() {
        return this.isTargetsLoading || this.isLayoutsLoading;
    }

    private changeFile(e) {
        this.file = e.srcElement.files[0];
    }

    getFile(file:File): Promise<string> {
        let reader = new FileReader();

        return new Promise<string>((resolve, reject) => {

            reader.onloadend = () => {
                resolve(reader.result);
            }
            reader.readAsDataURL(file);
        }).then(() => {            
            return reader.result;
        });
    }
};