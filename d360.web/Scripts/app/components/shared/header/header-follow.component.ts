import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FollowerService } from '../../../services/follower.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { MessageService } from 'primeng/api';//primeng/api


@Component({
    selector: 'd3s-header-follow',
    template:
        `
        <div *ngIf="visible" class="show-on-medium-and-down hide-on-med-and-up" (click)="toggleFollow()">
            <div class="mini-menu-line">
                <div class="check-gutter">
                    <i *ngIf="active && !isLoading" class="fa fa-check"></i>
                    <i *ngIf="isLoading" style="color: #000;" class="fa fa-spinner fa-spin"></i>
                </div>
                <div class="text" i18n>Follow</div>
                <div class="expand-gutter"></div>            
            </div>
        </div>
        <div *ngIf="visible" (click)="toggleFollow()" [class.active]="active" class="header-button hide-on-med-and-down" [title]="tooltipString">
            <i *ngIf="!isLoading" class="fa fa-bookmark"></i>
            <i *ngIf="isLoading" class="fa fa-spinner fa-spin"></i>
        </div>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush
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
        protected headerActionsService: HeaderActionsService,
        private ref: ChangeDetectorRef,
        private messageService: MessageService
    ) { }

    ngOnInit() {

        this.sub = this.breadcrumbService.currentObjectInfo$.subscribe(c => {
            this.objectType = c.type;
            this.objectId = c.id;
            this.checkActive();
        });

        //set values on initial load
        let o = this.breadcrumbService.currentObject;
        if (o != null) {
            this.objectType = o.type;
            this.objectId = o.id;
            this.checkActive();
        }
    }


    checkActive() {
        this.active = false;
        this.visible = true;
        if (this.objectType == null || this.objectType == "" || this.objectId < 0) {
            this.visible = false;
            return;
        }


        this.followerService.getFollowInfo(this.objectType, this.objectId).subscribe(
            f => {
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
            }
        );
    }

    toggleFollow() {
        if (this.isFollowingParent && (this.objectType != this.parentObjectType || this.objectId != this.parentObjectId)) {
            this.messageService.add({ severity: 'info', summary: 'Following Parent', detail: 'Following via Parent.\nTo unfollow, please go to type list.' });
            return;
        }
        if (this.objectType == null || this.objectType == "" || this.objectId < 0) {
            return;
        }
        this.isLoading = true;
        let includeChildren = this.objectType.endsWith('Type');

        this.followerService.updateFollowStatus(this.objectType, this.objectId, includeChildren).subscribe(
            f => {
                if (f.type == 'notification') {
                    this.active = !this.active;
                    let crumbs = this.breadcrumbService.getBreadcrumbsFromStorage();
                    let toastMessage = `You are now following '${crumbs[crumbs.length - 1].text}'`;
                    let toastTitle = "Followed";
                    if (this.active) {
                        if (includeChildren) {
                            toastTitle = "Following Type";
                            toastMessage = `You are now following type '${crumbs[crumbs.length - 1].text}'`;
                        }
                    } else {
                        if (includeChildren) {
                            toastTitle = "Unfollowed Type";
                            toastMessage = `You have unfollowed type '${crumbs[crumbs.length - 1].text}'`;
                        } else {
                            toastMessage = `You have unfollowed '${crumbs[crumbs.length - 1].text}'`;
                            toastTitle = "Unfollowed";
                        }
                    }

                    this.messageService.add({ severity: 'info', summary: toastTitle, detail: toastMessage });
                    this.checkActive();
                }

                this.ref.markForCheck();

                this.isLoading = false;
            }
        );
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
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
        this.ref.markForCheck();
    }
}

