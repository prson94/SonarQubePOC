import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { Resource, CountObject } from '../../../models/resource.model';
import { SocialCommentType } from '../../../models/social.model';
import { ResourcesService } from '../../../services/resources.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-resource-following',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">
                        <d3s-social-board *ngIf="showBoard" [objectType]="''" [objectID]="resourceId" [objectName]="firstName" [daysToLookBack]="daysToLookBack"></d3s-social-board>
                    </div>
                </div>
            </div>
        `,
    providers: [ResourcesService]
})

export class ResourceCommentsComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() resourceId: any = 0;
    @Input() resource: Resource = null;

    private sub: any;
    daysToLookBack: number = 90;
    limitToType: SocialCommentType = SocialCommentType.Social;
    hasCloseButton: boolean = false;
    showBoard: boolean = false;
    firstName: string = '';

    constructor(private route: ActivatedRoute, private resourcesService: ResourcesService, private router: Router) { super(); }

    ngOnInit() {

        this.isLoading = true;
        this.showBoard = false;

        this.sub = this.route.params.subscribe(params => {
            this.resourceId = +params['resourceID'];
            this.resourcesService.getResource(this.resourceId)
                .then(r => {
                    this.resource = r;
                    this.firstName = r.FirstName;
                    this.isLoading = false;
                    this.showBoard = true;
                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}