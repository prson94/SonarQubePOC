import { Component, Input } from "@angular/core";
import { Breadcrumb } from "../../../models/breadcrumb.model";
import { HeaderBreadcrumbService } from "../../../services/header-breadcrumb.service";


@Component({
    selector: 'd3s-use-breadcrumbs',
    template: ``,
})
export class UseBreadcrumbsComponent {
    @Input() breadcrumbs: Breadcrumb[];

    constructor(
        private headerBreadcrumbService: HeaderBreadcrumbService
    ) {
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearBreadcrumbs();
    }

    ngOnChanges() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        for (let breadcrumbItem of this.breadcrumbs ?? []) {
            this.headerBreadcrumbService.showBreadcrumb(breadcrumbItem);
        }
    }

    ngOnDestroy() {
        this.headerBreadcrumbService.clearBreadcrumbs();
    }
}