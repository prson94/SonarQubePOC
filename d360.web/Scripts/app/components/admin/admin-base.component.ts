import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService } from '../../services/messages.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';

import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';



export class AdminBaseComponent extends BaseComponent {
    public areaName: string;
    public areaLink: string = undefined;    
    public area: string = "Administration";

    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected titleService: Title, rightSidebarService?: RightSidebarService) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    setCommonItems() {
        this.area = ['Artifacts'].indexOf(this.areaName) !== -1 ? 'Configuration' : "Administration";
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area));
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.areaName, this.areaLink));     
        this.setBrowserTitle(this.titleService, this.areaName);
    }       
}