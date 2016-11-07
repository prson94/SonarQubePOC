import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { FollowerService } from '../../services/index';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderActionsService } from '../../services/header-actions.service';
import * as _ from 'lodash';


@Component({
    selector: 'd3s-header-follow',    
    template:
    `
        <span *ngIf="visible" (click)="toggleFollow()" [class.active]="active" class="follow">
            <i *ngIf="!isLoading" class="fa fa-bookmark"></i>
            <i *ngIf="isLoading" class="fa fa-spinner fa-spin" style="color:black;"></i>
        </span>
    `,
    providers: [FollowerService]
})

export class HeaderFollowComponent implements OnInit, OnDestroy {
    @Input() uri: string;
    @Input() active: boolean = false;
    @Output() onClick = new EventEmitter();

    visible: boolean = true;
    isFollowing: boolean = false;
    isFollowingParent: boolean = false;

    objectType: string = "";
    objectId: number = 0;

    isLoading = false;
    sub: any;

    private tooltipMessage: string = 'Follow this item';

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private followerService: FollowerService,
        private breadcrumbService: HeaderBreadcrumbService,
        protected headerActionsService: HeaderActionsService) { }

    ngOnInit() {

        this.sub = this.breadcrumbService.currentObjectInfo$.subscribe(c => {
            this.objectType = c.type;
            this.objectId = c.id;            
            this.checkActive();
        });        
    }

    checkActive() {
        this.active = false;
        this.visible = true;
        if (this.objectType == null || this.objectType == "" || this.objectId < 0) {
            this.visible = false;
            return;
        }

        this.followerService.getFollowInfo(this.objectType, this.objectId)
            .then(f => {
                this.isFollowing = f.isFollowing;
                this.isFollowingParent = f.isFollowingParent;

                if (f.isFollowing || f.isFollowingParent)
                    this.active = true;
            });
    }

    toggleFollow() {                
        if (this.isFollowingParent)
            return;
        if (this.objectType == null || this.objectType == "" || this.objectId < 0) {
            return;
        }

        let includeChildren = this.objectType.endsWith('Type');

        this.followerService.updateFollowStatus(this.objectType, this.objectId, includeChildren)
            .then(f => {                
                this.active = f;
            });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}

