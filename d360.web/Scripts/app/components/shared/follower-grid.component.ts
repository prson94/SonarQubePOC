import { Component, Input, Output, EventEmitter, OnInit, OnChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FollowerService } from '../../services/index';
import { FollowDetail } from '../../models/follower.model';

@Component({
    selector: 'd3s-follower-grid',
    template: `
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <header *ngIf="objectName">Followers of {{objectName}}</header>
                <span *ngIf="!isLoading">
                    <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">
                    <p-dataTable [globalFilter]="gb" [value]="items" [rows]="10" [paginator]="true" selectionMode="single">
                        <p-column field="FollowerLastName" header="Last Name" sortable="true"></p-column>
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


    isLoading = false;

    constructor(private followerService: FollowerService) {
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
}