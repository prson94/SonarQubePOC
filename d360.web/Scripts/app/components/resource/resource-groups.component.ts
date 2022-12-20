import { Component, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { ResourcesService } from '../../services/resources.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Resource } from '../../models/resource.model';
import { Observable } from 'rxjs';
import { AssetService } from '../../services/asset.service';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-resource-groups',
    providers: [ResourcesService, AssetService],
    templateUrl: './resource-groups.component.html'
})

export class ResourceGroupsComponent extends BaseComponent implements OnInit{
    @Input() resourceUid: string;

    private groups: any[];
    private user: Observable<Resource>;
    private id: number;

    constructor(
        private assetService: AssetService,
        private resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);        
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.resourcesService.getUserGroups(this.resourceUid)
            .subscribe((res) => {
                this.groups = res.items;
                this.isLoading = false;
            });
    }

    private groupUrl(id) {
        return `${SiteUrlHelpers.SITE_URL_GROUP_ROOT}/${id}`;
    }

    private doSelect(group) {
        this.assetService.getAssetLegacyUri(group.Uid).subscribe((uri) => {
                this.router.navigateByUrl(uri);
            });
       
    }
}