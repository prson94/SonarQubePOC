import { Component, OnInit } from '@angular/core';
import { BaseComponent } from '../../components/shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { CompanySettingsService } from '../../services/settings.service';
import { D3SSharedModule } from '../../components/shared/shared.module';
import { LoadingComponent } from '../../_shared/components/loading';
import { HighchartsChartModule } from 'highcharts-angular';
import * as Highcharts from 'highcharts';
import { ResponsibilityTypeCount } from '../../models/responsibility-type.model';

@Component({
    selector: 'community',
    styleUrls: ['index.less'],
	templateUrl: 'index.html',
	standalone: true,
	imports: [D3SSharedModule, HighchartsChartModule, LoadingComponent]
})

export class CommunityIndex extends BaseComponent implements OnInit {
    //responsibilitiesPie: Object;
    selectedResponsibilityUid: string = "";
    selectedResponsibilityName: string;
    selectedResource: any;

	options: Highcharts.Options = {};
	Highcharts: typeof Highcharts = Highcharts;
	chartData: ResponsibilityTypeCount[];

    constructor(protected responsibilityTypeService: ResponsibilityTypeService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, $localize`Community`);
        this.headerBreadcrumbService.getFolderTitle('#Community').then((res) => {
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.clearCurrentObjectInfo();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res));

            this.headerBreadcrumbService.getFolderIcon(res).subscribe((icon) => {
                this.clearSidebar();
                this.secondaryNavService.setCurrentArea(res, icon, $localize`Community`);
                this.secondaryNavService.clearCurrentObject();
            });
            this.secondaryNavService.showHeader(true);

        });
        this.load();
    }


    private load() {
        this.isLoading = true;
        this.responsibilityTypeService.getResponsibilityTypeBreakdown().
			subscribe((result) => {

				this.chartData = result;

				this.options = {
					chart: {
						plotBackgroundColor: null,
                        plotBorderWidth: null,
                        plotShadow: false,
                        type: 'pie',
                        backgroundColor: 'transparent',
                        style: {
                            fontFamily: 'Precisely'
                        }
                    },
                    title: {
                        text: null
                    },
                    subtitle: {
                        text: $localize`Click on a pie piece for more details.`
                    },
                    credits: {
                        enabled: false
                    },
                    tooltip: {
                        formatter () {
                            return this.point.name + '<br>' + Highcharts.numberFormat(this.y, 0, '.', ',') + ' ' + $localize`Total Assigned Items`;
                        }
                    },
                    plotOptions: {
                        pie: {
                            allowPointSelect: true,
                            cursor: 'pointer',
                            dataLabels: {
                                enabled: true,
                                formatter () {
                                    return '<b>' + this.point.name + '</b>: ' + Highcharts.numberFormat(this.y, 0, '.', ',');
                                }
                            }
                        }
					},
					responsive: {
						rules: [{
							condition: {
								maxWidth: 500
							},
							chartOptions: {
								subtitle: { text: null }
							}
						}]
					},
                    series: [{
						name: 'Responsibilities',
						type: 'pie',
                        //colorByPoint: true,
                        data: result.map((x) => ({
                            name: x.ResponsibilityType,
                            y: x.Count
                        })),
                        events: {
                            click: function (e) { this.onPieClick(e); }.bind(this)
                        }
                    }]
                };

                //Highcharts.chart('responsibilitiesPie', options);

                this.isLoading = false;
            });
    }

    onPieClick(e) {
        this.selectedResource = null;
		this.selectedResponsibilityName = e.point.name; //name

		const selectedOwnerType = this.chartData.find(o => o.ResponsibilityType === e.point.name);
		this.selectedResponsibilityUid = selectedOwnerType.ResponsibilityTypeUID; // triggers user responsibilities piece to load.    
    }

    GetHeadingLabel() {
        return $localize`Users Assigned As ${this.selectedResponsibilityName}`;
    }
}