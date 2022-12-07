import { Component, Input } from "@angular/core";
import { Subscription } from "rxjs";
import { skip } from "rxjs/operators";
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

    isChangingBreadcrumbs = false;
    breadcrumbsSubscription: Subscription;

    ngOnInit() {
        this.setBreadcrumbs([]);
        this.breadcrumbsSubscription = this.headerBreadcrumbService.breadcrumbs$
            .pipe(skip(1))
            .subscribe(() => {
                if (this.isChangingBreadcrumbs) {
                    return;
                }

                this.setBreadcrumbs(this.breadcrumbs);
            });
    }

    ngOnChanges() {
        this.setBreadcrumbs(this.breadcrumbs);
    }

    setBreadcrumbs(breadcrumbs: Breadcrumb[]) {
        this.isChangingBreadcrumbs = true;
        try {
            this.headerBreadcrumbService.clearBreadcrumbs();
            for (const breadcrumbItem of breadcrumbs ?? []) {
                this.headerBreadcrumbService.showBreadcrumb(breadcrumbItem);
            }
        } finally {
            this.isChangingBreadcrumbs = false;
        }
    }

    ngOnDestroy() {
        this.setBreadcrumbs([]);
        this.breadcrumbsSubscription.unsubscribe();
    }
}
