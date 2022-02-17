import {Component, Input, OnInit, OnDestroy} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {BaseComponent} from '../../shared/base.component';
import {PermissionsService} from '../../../services/permissions.service';
import {ObjectDetailService} from '../../../services/object-detail.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-relationships-wrapper',
    template: `
                    <d3s-object-relationships *ngIf="false" [objectType]="objectType"
                                              [objectID]="objectID"
                                              [objectName]="objectName"
                                              [objectPermissions]="permissions"></d3s-object-relationships>

                    <gov-relationship-detail [assetUid]="assetUid"></gov-relationship-detail>
    `,
    providers: [PermissionsService, ObjectDetailService]
})

export class RelationshipsComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    assetUid: string = '';

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private permissionsService: PermissionsService,
        private objectDetailService: ObjectDetailService,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];

            this.objectDetailService.getObject(this.objectID, this.objectType).subscribe(
                res => {
                    this.objectName = res.Name ? res.Name : res.DisplayValue;
                    this.assetUid = res["Uid"];
                }
            );
            this.loadPermissions(this.permissionsService, this.objectType, this.objectID);
            this.buildSecondaryNavigation(null, this.objectID, this.objectType);
        });
    }


    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}
