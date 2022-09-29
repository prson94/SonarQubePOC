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

	assetUId: string = "";
	assetTypeUId: string = "";

	emptyguid: string = "00000000-0000-0000-0000-000000000000";
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

        this.sub = this.breadcrumbService.currentObjectInfo$.subscribe((c) => {
			this.assetTypeUId = c.AssetTypeUid;
			this.assetUId = c.AssetUid;
            this.checkActive();
        });

        //set values on initial load
        let o = this.breadcrumbService.currentObject;
        if (o != null) {
			this.assetTypeUId = o.AssetTypeUid;
			this.assetUId = o.AssetUid;
            this.checkActive();
        }
    }


    checkActive() {
        this.active = false;
        this.visible = true;
		if ((this.assetUId === null || this.assetUId === "" || typeof this.assetUId === "undefined") && (this.assetTypeUId === null || this.assetTypeUId === "" || typeof this.assetTypeUId === "undefined")) {
			this.visible = false;
			return;
		}

		if (typeof this.assetUId === "undefined" || this.assetUId === null) {
			this.assetUId = this.emptyguid;
		}
		if (typeof this.assetTypeUId === "undefined" || this.assetTypeUId === null) {
			this.assetTypeUId = this.emptyguid;
		}

		this.followerService.getFollowInfo(this.assetUId, this.assetTypeUId).subscribe(
            (f) => {
                this.isFollowing = f.isFollowing;
                this.isFollowingParent = f.isFollowingParent;

                this.updateTooltip();
            }
        );
    }

    toggleFollow() {
		if (this.isFollowingParent && (this.assetUId !== this.emptyguid)) {
            this.messageService.add({ severity: 'info', summary: 'Following Parent', detail: 'Following via Parent.\nTo unfollow, please go to type list.' });
            return;
        }
		if (this.assetUId === this.emptyguid && this.assetTypeUId === this.emptyguid) {
            return;
        }
        this.isLoading = true;
		let includeChildren = false;
		if (this.assetTypeUId !== this.emptyguid && this.assetUId === this.emptyguid) {
			includeChildren = true;
		}

		this.followerService.updateFollowStatus(this.assetUId, this.assetTypeUId, includeChildren).subscribe(
            (f) => {
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
            this.tooltipString = $localize`Stop following`;
        else if (!this.isFollowingParent && !this.isFollowing)
            this.tooltipString = $localize`Follow this item`;
		else if (this.isFollowingParent && this.assetTypeUId !== this.emptyguid)
            this.tooltipString = $localize`Stop following`;
		else if (this.isFollowingParent && this.assetTypeUId === this.emptyguid)
            this.tooltipString = $localize`Following parent item`;
        this.ref.markForCheck();
    }
}

