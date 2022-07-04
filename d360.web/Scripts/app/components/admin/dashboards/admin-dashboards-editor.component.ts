import { Input, Component, EventEmitter, Output } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { Report } from '../../../models/report.model';
import { DropdownOption } from '../../../models/dropdown.model';
import * as _ from 'lodash';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';
import { DashboardService } from '../../../services/dashboard.service';
import { DashboardDefinition, DashboardLocation, DashboardModel, DashboardType } from '../../../models/dashboard.model';

@Component({
    selector: 'd3s-admin-dashboards-editor',
	templateUrl: 'admin-dashboards-editor.component.html',
	providers: [DashboardService, ResponsibilityTypeService],
})

export class AdminDashboardsEditor {
    @Input() report: DashboardModel;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = $localize`Edit`;
    error: any;
	editedReport: DashboardModel;
    isTargetsLoading: boolean = false;
    reportTypes: DropdownOption[] = [];
    targetTypes: DropdownOption[] = [];
    responsibilities: SelectItem[] = [];
    file: File;

    labelSave = $localize`Save`;
    labelClose = $localize`Close`;

	showOnHomePage: boolean = false;

    constructor(
		private dashboardService: DashboardService,
        private responsibilityTypeService: ResponsibilityTypeService,
        protected settingsService: CompanySettingsService
    ) {
		this.reportTypes.push({ value: "PowerBi", title: "PowerBI" });
    }

    ngOnInit() {
        let enableDqPlus = this.settingsService.getSettingById(CompanySettingEnum.EnableSagacity).BooleanSetting.Value;
        if (enableDqPlus) {
			this.reportTypes.push({ value: "DqPlus", title: "Data360 DQ+" });
        }
        if (this.report != undefined) {
            this.editedReport = _.cloneDeep(this.report);
			this.objectTypeChanged(this.editedReport.AssetTypeUid, true);
        }
        else {
			this.editedReport = new DashboardModel();
			this.editedReport.Definition = new DashboardDefinition();
            this.action = $localize`New`;
        }
        this.getReportTargets();
    }

    reportTypeChange() {
		this.file = null;
		this.editedReport.Responsibilities = [];
    }

	onSubmit() {
		var objectTypeData = this.editedReport.SelectedObjectData.split('|');
		this.editedReport.AssetTypeUid = objectTypeData[0];
		this.editedReport.Location = DashboardLocation.List;
		
		if (objectTypeData[1]) {
			this.editedReport.Location = DashboardLocation.Detail;
		}
		if (this.showOnHomePage) {
			this.editedReport.Location = DashboardLocation.Homepage;
		}

        this.saveClick.emit({ report: this.editedReport, action: this.report ? "new" : "edit", file: this.file });
    }

    getReportTargets() {
        this.isTargetsLoading = true;
		this.dashboardService.getReportTargetTypes()
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
		if (this.action === $localize`New` && this.editedReport.DashboardType == DashboardType.PowerBi.toString())
            return this.file != null
        else
            return true;
    }

    private objectTypeChanged(assetTypeUid: string, isInitialLoad?: boolean) {
        if (!isInitialLoad) {
            this.editedReport.Responsibilities = [];
        }
		var objectTypeData = this.editedReport.SelectedObjectData.split('|');
		this.editedReport.AssetTypeUid = objectTypeData[0];

		this.responsibilityTypeService.getAdminResponsibilityTypes(this.editedReport.AssetTypeUid)
			.subscribe((res) => {
				this.responsibilities = [];
				res.forEach((o) => {
					this.responsibilities.push({
						label: o.Name,
						value: o.uid
					});
				});
			});
    }
}