import { Input, Component, EventEmitter, Output } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { ReportsService} from '../../../services/reports.service';
import { Report } from '../../../models/report.model';
import { DropdownOption } from '../../../models/dropdown.model';
import * as _ from 'lodash';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';

@Component({
    selector: 'd3s-admin-dashboards-editor',
    template: ` 
                <header>{{action}} Report</header>
                <d3s-loading [isLoading]="isLoading()"></d3s-loading>
                <div class="row" *ngIf="!isLoading()">
                    <div class="form-instructions">Add a report to the list of reports, which can then be exposed in other areas of this system.</div>            
                    <form (ngSubmit)="onSubmit()" #reportForm="ngForm">                        
                        <div class="col s12">
                            <div class="FieldName">Report Type</div>
                            <div>                                
                                <select required [(ngModel)]="editedReport.ReportType" name="reportType" #reportType="ngModel" (ngModelChange)="reportTypeChange()" style="width:100%;">
                                  <option *ngFor="let p of reportTypes" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>       
                            <div [hidden]="reportType.valid || reportType.pristine">A report type is required</div>                     
                        </div>                        
                        <div class="col s12">
                            <div class="FieldName">Name</div>
                            <div><input required style="width: 100%;" name="name" type="string" [(ngModel)]="editedReport.Name" #name="ngModel" maxlength="250"></div>     
                            <div [hidden]="name.valid || name.pristine">A name is required</div>                                                   
                        </div>     
                        <div class="col s12">
                            <div class="FieldName">Target Type</div>
                            <div>                                
                                <select required [ngModel]="editedReport.ObjectType" (ngModelChange)="editedReport.ObjectType=$event;objectTypeChanged($event);" name="targetType" #targetType="ngModel" style="width:100%;">
                                  <option *ngFor="let p of targetTypes" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>       
                            <div [hidden]="targetType.valid || targetType.pristine">A target type is required</div>                                                                        
                        </div>
                        <div class="col s12" *ngIf="editedReport.ReportType == 'sagacity'">
                            <div class="FieldName">Url</div>
                            <div>
                                <input required style="width: 100%;" name="url" type="string" [(ngModel)]="editedReport.Url" #name="ngModel" maxlength="500">
                            </div>
                        </div>
                        <div class="col s12" *ngIf="editedReport.ReportType == 'sagacity' || editedReport.ReportType == 'powerbi'">
                            <div class="FieldName">Show on Home Page?</div>
                            <div>
                                <input name="showOnHomePage" type="checkbox" [(ngModel)]="editedReport.ShowOnHomePage" #name="ngModel" />
                            </div>
                        </div>
                        <div class="col s12" *ngIf="editedReport.ReportType == 'powerbi'">
                            <div class="FieldName">File</div>
                            <div><input type="file" (change)="changeFile($event);" accept=".pbix" style="width: 99%" name="File" formenctype="multipart/form-data" /></div>
                             <div class="errorMessage" [hidden]="isValid()">File is required</div>
                        </div>
                        <div class="col s12" *ngIf="editedReport.ReportType == 'powerbi' && !editedReport.ShowOnHomePage && editedReport.ObjectType">
                            <div class="FieldName">Restrict Visibility To</div>
                            <div><p-multiSelect [options]="responsibilities" placeholder="Choose" [(ngModel)]="editedReport.VisibleToRoles" [style]="{'width':'100%'}" name="responsibilities" selectedItemsLabel="{0} items selected"></p-multiSelect></div>
                        </div>
                        <div class="col s12">
                            <div class="FieldName">Description</div>
                            <p-editor name="description" [style]="{'height':'150px'}" [(ngModel)]="editedReport.Description"></p-editor>
                        </div>                    
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!reportForm.form.valid || !isValid()" style="width: '150px';" label="Save"></button>                            
                            <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: '150px';"></button>
                        </div>                    
                    </form>                           
                </div>
                `,
    providers: [ReportsService, ResponsibilityTypeService],
})

export class AdminDashboardsEditor {
    @Input() report: Report;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedReport: Report;
    isTargetsLoading: boolean = false;
    reportTypes: DropdownOption[] = [];
    targetTypes: DropdownOption[] = [];
    responsibilities: SelectItem[] = [];
    file: File;

    constructor(
        private reportsService: ReportsService,
        private responsibilityTypeService: ResponsibilityTypeService,
        protected settingsService: CompanySettingsService
    ) {
        this.reportTypes.push({ value:"powerbi", title:"PowerBI" });
    }

    ngOnInit() {
        let enableDqPlus = this.settingsService.getSettingById(CompanySettingEnum.EnableSagacity).BooleanSetting.Value;
        if (enableDqPlus) {
            this.reportTypes.push({ value: "sagacity", title: "Data360 DQ+" });
        }
        if (this.report != undefined) {
            this.editedReport = _.cloneDeep(this.report);
            this.editedReport.ObjectType = this.editedReport.ObjectType + '|' + this.editedReport.ObjectID.toString();
            this.objectTypeChanged(this.editedReport.ObjectType,true);
        }
        else {
            this.editedReport = new Report();
            this.action = "New";
        }
        this.getReportTargets();
    }

    reportTypeChange() {
        this.file = null;
    }

    onSubmit() {
        this.saveClick.emit({ report: this.editedReport, action: this.report ? "new" : "edit", file: this.file });
    }

    getReportTargets() {
        this.isTargetsLoading = true;
        this.reportsService.getReportTargetTypes()
            .subscribe(result => {
                this.targetTypes = result;
                this.isTargetsLoading = false;
            });
    }

    isLoading() {
        return this.isTargetsLoading;
    }

    private changeFile(e) {
        this.file = e.srcElement.files[0];
    }

    private isValid(): boolean {
        if (this.action === "New" && this.editedReport.ReportType == "powerbi")
            return this.file != null
        else
            return true;
    }

    private objectTypeChanged(type: string, isInitialLoad?: boolean) {
        let object = type.split("|");
        if (!object || object.length < 2) {
            console.log("ERROR - INVALID OBJECT INFO SPECIFIED.");
            return;
        }
        if (!isInitialLoad) {
            this.editedReport.VisibleToRoles = [];
        }
        let ot = object[0];
        if (!ot.endsWith("Type"))
            ot += "Type";
        let otid: number = +object[1];
        this.responsibilityTypeService.getRelationsByObjectType(ot, otid).
            subscribe((res) => {
                this.responsibilities = [];
                res.forEach((o) => {
                    this.responsibilities.push({
                        label: o.ResponsibilityTypeName,
                        value: o.ResponsibilityTypeID
                    });
                });
                if (isInitialLoad && this.editedReport && this.editedReport.VisibleTo) {
                    this.editedReport.VisibleToRoles = this.editedReport.VisibleTo.split(',');
                }
            });
    }
}