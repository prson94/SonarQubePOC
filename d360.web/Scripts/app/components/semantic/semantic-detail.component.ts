import { Input, Component, OnChanges, SimpleChange, ChangeDetectorRef, Output, EventEmitter, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { SemanticType } from '../../models/semantic-type.model';

import { DataProfileService } from '../../services/dataprofile.service';
import { ResourcesService } from '../../services/resources.service';
import { CompanySettingsService } from '../../services/settings.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'semantic-detail',
    templateUrl: './semantic-detail.component.html',
    styleUrls: ["semanticTypes.less"],
    providers: [DataProfileService]
})


export class SemanticDetailComponent extends BaseComponent implements OnChanges {
    @Input() qualifier: string="";
    @Input() isSidePanel: boolean = false;
    @Input() showHeader: boolean = false;
    @Input() semanticType: SemanticType;

    semanticDetails: SemanticType;
    semanticAssets: any[];
    showAssetsTab: boolean = true;
    tab: string = 'detail';
    advancedJson: string;
    creator: any;
    assetCount: number;


    constructor(        
        private router: Router,
        private dataProfileService: DataProfileService,
        private changeDetectorRef: ChangeDetectorRef,
        private resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.getData();
    }

    getData() {
        this.isLoading = true;
        if (this.semanticType) {
            this.semanticDetails = this.semanticType;            
        } else {
            this.dataProfileService.getSemanticTypes(1, 1, "", `qualifier eq '${this.qualifier}'`).subscribe((s) => {
                this.semanticDetails = s.items[0];
            });
        }

        if (SemanticType[this.semanticDetails.matchType] === SemanticType["Advanced"]) {
            this.advancedJson = JSON.stringify(this.semanticDetails.advanced);
        }

        this.dataProfileService.getSemanticTypeMatchingAssets(this.semanticType.qualifier, 1, 1, this.semanticType.threshold).subscribe((result) => {            
            this.assetCount = result.total;            
            this.isLoading = false;
        });
        
        this.getUserDetails();
        this.changeDetectorRef.markForCheck();
    }

    getUserDetails() {
        let createdByUserParam = {};
        createdByUserParam["Uid"] = this.semanticDetails.createdBy.uid;

        this.resourcesService.getResourceLazy(createdByUserParam)
            .subscribe((result) => {
                if (result) {
                    this.creator = result.items[0];
                    this.semanticDetails.createdBy.id = this.creator.ResourceID;
                    if (this.semanticDetails.createdBy.uid === this.semanticDetails.updatedBy.uid) {
                        this.semanticDetails.updatedBy.id = this.creator.ResourceID;
                    }
                }
            });
        if (this.semanticDetails.createdBy.uid !== this.semanticDetails.updatedBy.uid) {
            let updatedByUserParam = {};
            updatedByUserParam["Uid"] = this.semanticDetails.updatedBy.uid;
            this.resourcesService.getResourceLazy(updatedByUserParam)
                .subscribe((result) => {
                    if (result) {
                        this.creator = result.items[0];
                        this.semanticDetails.updatedBy.id = this.creator.ResourceID;
                    }
                });
        }        
    }

    navigateToUser(resourceID: number, newTab: boolean = false) {
        let url = `${SiteUrlHelpers.SITE_URL_RESOURCE_ROOT}/${resourceID}`;
        if (url) {
            if (newTab) {
                window.open(url, '_blank');
            } else {
                this.router.navigateByUrl(url);
            }
        }
    }

    openSemanticType(newTab: boolean = false) {
        let url = `${SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT}/${this.semanticDetails.uid}`;
        if (url) {
            if (newTab) {
                window.open(url, '_blank');
            } else {
                this.router.navigateByUrl(url);
            }
        }
    }

    clickTab(key: string) {
        this.tab = key;
    }   
}