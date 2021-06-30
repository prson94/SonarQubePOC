import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';

import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { ViewChildren, Component } from '@angular/core';
import { SecondaryNavCurrentObject } from '../../models/secondaryNav.model';
import { StringConstants } from '../../static/string-constants';


@Component({
    template: ''
})
export class AdminBaseComponent extends BaseComponent {
    public areaName: string;
    public areaLink: string = undefined;    
    public area: string = StringConstants.Area_Administration;
    public adminHeading: string;
    public tabTitle: string = 'Admin'

    @ViewChildren('treetableRows') treeTableElements: any;
    private isDefaultTreeValuesSet: boolean = false;


    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected titleService: Title, secondaryNavService?: SecondaryNavService) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerBreadcrumbService;
    }

    setCommonItems(showAreaAsType: boolean = false, headerOverride: string = null) {

        this.area = [
            StringConstants.Section_BusinessAssets,
            StringConstants.Section_TechnicalAssets,
            StringConstants.Section_Artifacts,
            StringConstants.Section_Models,
            StringConstants.Section_Policies,
            StringConstants.Section_Predicates,
            StringConstants.Section_Relationships,
            StringConstants.Section_Rules,
            StringConstants.Section_Scoring,
            StringConstants.Section_Surveys,
            StringConstants.Section_Actions,
            StringConstants.Section_Workflows
        ].indexOf(this.areaName) !== -1 ? StringConstants.Area_Configuration : StringConstants.Area_Administration;

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