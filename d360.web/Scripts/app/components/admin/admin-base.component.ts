import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService  } from '../../services/index';

export class AdminBaseComponent {

    constructor(private areaName : string, headerBreadcrumbService : HeaderBreadcrumbService) {
        headerBreadcrumbService.clearBreadcrumbs();
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Administration", ""));
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb(areaName, ""));
    }
}