import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';

import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { ViewChildren, Component } from '@angular/core';
import { SecondaryNavCurrentObject } from '../../models/secondaryNav.model';
import { StringConstants } from '../../static/string-constants';
import { CompanySettingsService } from '../../services/settings.service';


@Component({
    template: ''
})
export class AdminBaseComponent extends BaseComponent {
    public areaName: string;
    public areaLink: string = undefined;    
    public area: string = StringConstants.Area_Administration;
    public adminHeading: string;
    public tabTitle: string = $localize`Admin`;

    @ViewChildren('treetableRows') treeTableElements: any;
    private isDefaultTreeValuesSet: boolean = false;

    constructor(
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected titleService: Title,
        protected settingsService: CompanySettingsService,
        secondaryNavService?: SecondaryNavService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerBreadcrumbService;
    }

    setCommonItems(showAreaAsType: boolean = false, headerOverride: string = null) {

        this.area = this.determineAreaForAdminPage(this.areaName);
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area));
        if (this.adminHeading)
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.adminHeading));     
        
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.areaName, this.areaLink, null, null, null, null, null, showAreaAsType));
        this.setBrowserTitle(this.titleService, this.areaName);
        this.secondaryNavService.clearItems();
        this.secondaryNavService.clearButtons();
        this.secondaryNavService.setCurrentArea(headerOverride ? headerOverride : this.areaName, this.area === StringConstants.Area_Configuration ? 'fa-sliders' : "fa-cog", this.tabTitle);
        this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject(null,null,null,null,true));
        this.secondaryNavService.showHeader(true);
    }       


    //Prime NG tree table doesnt handle default values good, trigger click on first element in p-treetable to mark it as seletected
    ngAfterContentChecked() {
        if (this.treeTableElements !== undefined && !this.isDefaultTreeValuesSet) {
            if (!this.treeTableElements.some((x) => x.nativeElement.className.includes('p-highlight'))) {
                this.treeTableElements.map((x, index) => {
                    if (index == 0) {
                        x.nativeElement.click();
                        this.isDefaultTreeValuesSet = true;
                    }
                });
            }
        }
    }

}