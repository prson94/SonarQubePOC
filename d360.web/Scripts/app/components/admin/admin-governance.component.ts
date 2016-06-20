///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { PageHeader} from '../../services/page-header.service';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { DataTable, Column } from 'primeng/primeng';
import { GovernanceItem, IGovernanceService } from '../../models/governance.model';
import { GovernanceService } from '../../services/governance.service';

@Component({
    selector: 'admin-governance',
    providers: [GovernanceService],
    directives: [ObjectDetailTile, DataTable, Column],
    templateUrl: 'scripts/app/components/admin/admin-governance.component.html',
})

export class AdminGovernanceComponent {
    isLoading = false; 

    private governanceItems = new Array<GovernanceItem>();
    private selectedRow = new GovernanceItem();

    constructor(private governanceService: GovernanceService, private pageHeader: PageHeader, private headerBreadcrumbService: HeaderBreadcrumbService) {
        this.pageHeader.title = 'Responsibility Types';
        this.pageHeader.description = 'Assign which objects can be owned, and whether groups, users or both may own them. You may also define application and licensing source types.';

        headerBreadcrumbService.clearBreadcrumbs();
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Administration", ""));
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Responsibility Types", ""));

        this.load();
        //console.log(this);
    }

    load(): void {

        this.governanceService.getGovernanceItems()
            .then(data => {
                this.governanceItems = data;
            });
    }
}