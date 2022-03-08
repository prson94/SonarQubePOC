import { Component, Input, OnInit, OnDestroy, EventEmitter, Output, ViewChild, ViewEncapsulation } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { PermissionsService } from '../../../services/permissions.service';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { ObjectRelationshipsComponent } from './object-relationships.component';
import { CompanySettingsService } from '../../../services/settings.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-relationships-modal',
    templateUrl: `./relationships-modal.component.html`,
    providers: [PermissionsService, ObjectDetailService]
})

export class RelationshipsModalComponent extends BaseComponent implements OnDestroy {
    private sub: any;

    @Input() assetUid: number;
    @Input() assetTypeUid: string;
    @Input() isModalVisible: boolean = false;
    @Input() subtitle: string;
    @Output() onClose = new EventEmitter;


    @ViewChild(ObjectRelationshipsComponent, { static: false }) private relationComponent: ObjectRelationshipsComponent;

    componentTitle: string = 'Relationships';


    constructor(
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        breadcrumbService: HeaderBreadcrumbService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnDestroy(): void {
        this.cancel();
    }

    closeRelationshipComponent() {
        if (this.relationComponent) {
            this.relationComponent.ngOnDestroy();
        }
    }

    cancel() {
        this.closeRelationshipComponent();
        this.isModalVisible = false;
        this.onClose.emit(null);
    }
}
