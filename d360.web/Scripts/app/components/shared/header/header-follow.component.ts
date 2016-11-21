import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { FollowerService } from '../../../services/index';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderActionsService } from '../../../services/header-actions.service';
import * as _ from 'lodash';


@Component({
    selector: 'd3s-header-follow',
    styles: [
        `
            .follow {
                font-size: 1.2em;
                color: #666;
                padding: 0 15px;
            }

            .follow.active {
                color: #0376c4;
            }
        `
    ],
    template:
    `
        <span *ngIf="visible" (click)="toggleFollow()" [class.active]="active" class="follow" [title]="tooltipString">
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

    parentObjectType: string = "";
    parentObjectId: number = 0;

    isLoading = false;
    sub: any;

    private tooltipString: string = 'Stop following';

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
            //console.log(c);
            this.checkActive();
        });

        //set values on initial load
        let o = this.breadcrumbService.currentObject;
        if (o != null) {
            this.objectType = o.type;
            this.objectId = o.id;
        }
        this.checkActive();

        //console.log(o);

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
                //console.log('getFollowInfo', f);
                this.isFollowing = f.isFollowing;
                this.isFollowingParent = f.isFollowingParent;

                if (f.parent) {
                    this.parentObjectType = f.parent.ObjectType;
                    this.parentObjectId = f.parent.ObjectID;
                } else {
                    this.parentObjectType = '';
                    this.parentObjectId = 0;
                }
                this.updateTooltip();
            });


    }

    toggleFollow() {

        //console.log('follow', this.isFollowingParent, this.objectType, this.objectId);
        if (this.isFollowingParent && (this.objectType != this.parentObjectType || this.objectId != this.parentObjectId))
            return;
        if (this.objectType == null || this.objectType == "" || this.objectId < 0) {
            return;
        }
        this.isLoading = true;
        let includeChildren = this.objectType.endsWith('Type');

        this.followerService.updateFollowStatus(this.objectType, this.objectId, includeChildren)
            .then(f => {
                //console.log(f);
                if (f.type == 'notification') {
                    this.active = !this.active;
                    this.checkActive();
                }
                this.isLoading = false;
            });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    updateTooltip() {
        if (this.isFollowing || this.isFollowingParent)
            this.active = true;
        if (!this.isFollowingParent && this.isFollowing)
            this.tooltipString = 'Stop following';
        else if (!this.isFollowingParent && !this.isFollowing)
            this.tooltipString = 'Follow this item';
        else if (this.isFollowingParent && this.objectType.endsWith('Type'))
            this.tooltipString = 'Stop following';
        else if (this.isFollowingParent && !this.objectType.endsWith('Type'))
            this.tooltipString = 'Following parent item';
    }
}

