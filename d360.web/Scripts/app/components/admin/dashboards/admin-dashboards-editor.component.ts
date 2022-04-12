import { Input, Component, EventEmitter, Output } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { ReportsService } from '../../../services/reports.service';
import { Report } from '../../../models/report.model';
import { DropdownOption } from '../../../models/dropdown.model';
import * as _ from 'lodash';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';

@Component({
    selector: 'd3s-admin-dashboards-editor',
    templateUrl: 'admin-dashboards-editor.component.html',
    providers: [ReportsService, ResponsibilityTypeService],
})

export class AdminDashboardsEditor {
    @Input() report: Report;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = $localize`Edit`;
    error: any;
    editedReport: Report;
    isTargetsLoading: boolean = false;
    reportTypes: DropdownOption[] = [];
    targetTypes: DropdownOption[] = [];
    responsibilities: SelectItem[] = [];
    file: File;

    labelSave = $localize`Save`;
    labelClose = $localize`Close`;

    constructor(
        private reportsService: ReportsService,
        private responsibilityTypeService: ResponsibilityTypeService,
        protected settingsService: CompanySettingsService
    ) {
        this.reportTypes.push({ value: "powerbi", title: "PowerBI" });
    }

    ngOnInit() {
        let enableDqPlus = this.settingsService.getSettingById(CompanySettingEnum.EnableSagacity).BooleanSetting.Value;
        if (enableDqPlus) {
            this.reportTypes.push({ value: "sagacity", title: "Data360 DQ+" });
        }
        if (this.report != undefined) {
            this.editedReport = _.cloneDeep(this.report);
            this.editedReport.ObjectType = this.editedReport.ObjectType + '|' + this.editedReport.ObjectID.toString();
            this.objectTypeChanged(this.editedReport.ObjectType, true);
        }
        else {
            this.editedReport = new Report();
            this.action = $localize`New`;
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
        if (this.action === $localize`New` && this.editedReport.ReportType == "powerbi")
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