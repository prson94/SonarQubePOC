import { Component, Input, OnChanges, OnInit } from '@angular/core';
import { FollowingDetailForResource } from '../../models/resource.model';
import { ResourcesService } from '../../services/resources.service';
import { FormHelper } from '../../models/form.model';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-resource-following-grid-tile',
    templateUrl: 'resource-following-grid.tile.component.html'
})
export class ResourceFollowingGridTile implements OnInit, OnChanges {
    @Input() resourceId: number;
    @Input() objectId: number;
    @Input() objectType: string;

    @Input() simpleFilter: boolean = false;

    isLoading = false;
    private items: FollowingDetailForResource[] = new Array<FollowingDetailForResource>();

    constructor(private resourcesService: ResourcesService, private router: Router) {
        
    }

    ngOnInit() { }

    ngOnChanges() {
        this.load();
    }


    load() {
        this.isLoading = true;
        this.resourcesService.getFollowingByResourceByType(this.resourceId, this.objectType, this.objectId)
            .subscribe((r) => {
                this.items = r;
                FormHelper.convertToNgUrl(this.items, 'Url');
                //console.log(r);
                this.isLoading = false;
            });
    }

    navigate(e: any) {
		const url = e.data.Url;
		this.router.navigateByUrl(SiteUrlHelpers.federateUrl(url));

    }
}