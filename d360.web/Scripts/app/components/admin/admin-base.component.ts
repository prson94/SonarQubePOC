import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService, PageHeader  } from '../../services/index';

export class AdminBaseComponent {
    public areaName: string;
    public areaDescription: string = "base";
    public area: string = "Administration";

    protected isLoading = false;

    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected pageHeader: PageHeader) {
        
    }

    setCommonItems() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area, ""));
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.areaName, ""));
        this.pageHeader.description = this.areaDescription;
    }
}