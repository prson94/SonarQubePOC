
import { Component, Input, Output, EventEmitter, OnInit, OnChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FollowerService } from '../../services/index';
import { FollowDetail } from '../../models/follower.model';

@Component({
    selector: 'd3s-follower-grid',
    template: `
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <p-dataTable [value]="items" [rows]="10" [paginator]="true" selectionMode="single">
        <p-column field="FollowerLastName" header="Last Name"></p-column>
        <p-column field="FollowerFirstName" header="First Name"></p-column>
    </p-dataTable>
</div>
        `,
    providers: [FollowerService],
})

export class FollowerGridComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;

    private items: FollowDetail[] = new Array<FollowDetail>();


    isLoading = false;

    constructor(private followerService: FollowerService) {
        super();
    }

    ngOnInit() {
    }

    ngOnChanges() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.followerService.getFollowers(this.objectType, this.objectID)
            .then(r => {
                this.items = r;
                console.log(r);
                this.isLoading = false;
            });
    }
}