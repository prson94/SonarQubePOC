import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService, PageHeader  } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';

export class AdminBaseComponent extends BaseComponent {
    public areaName: string;
    public areaLink: string = undefined;
    public areaDescription: string = "base";
    public area: string = "Administration";
    

    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected pageHeader: PageHeader, protected titleService: Title) {
        super();        
    }

    setCommonItems() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area));
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.areaName, this.areaLink));
        this.pageHeader.description = this.areaDescription;
        this.setBrowserTitle(this.titleService, this.areaName);
    }
}