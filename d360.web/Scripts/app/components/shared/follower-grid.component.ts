import { Component, Input, Output, EventEmitter, OnInit, OnChanges } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { FollowerService } from '../../services/index';
import { FollowDetail } from '../../models/follower.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-follower-grid',
    template: `
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <header *ngIf="objectName">Followers of {{objectName}}</header>
                <span *ngIf="!isLoading">
                    <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                    <p-dataTable sortField="FollowerLastName" [sortOrder]="1" [globalFilter]="gb" [value]="items" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" selectionMode="single">
                        <p-column field="FollowerLastName" header="Last Name" sortable="true">
                            <template let-item="rowData" pTemplate type="body">
                                    <a (click)="doSelect(item)">{{item.FollowerLastName}}</a>
                                </template>
                        </p-column>
                        <p-column field="FollowerFirstName" header="First Name" sortable="true"></p-column>
                        <p-column [style]="{'width':'28px'}" >
                            <template let-item="rowData" pTemplate type="body">
                                <d3s-tooltip objectType="Resource" [objectId]="item.ResourceID" tooltipType="preview"><i class="fa fa-info"></i></d3s-tooltip>
                            </template> 
                        </p-column>     
                    </p-dataTable>
                </span>
        `,
    providers: [FollowerService],
})

export class FollowerGridComponent extends BaseComponent implements OnInit {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() objectName: string;

    private items: FollowDetail[] = new Array<FollowDetail>();

    constructor(private followerService: FollowerService, private router: Router) {
        super();
    }
    
    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.followerService.getFollowers(this.objectType, this.objectID)
            .then(r => {
                this.items = r;                
                this.isLoading = false;
            });
    }

    private doSelect(follower: FollowDetail) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('resource', follower.ResourceID));
    }
}