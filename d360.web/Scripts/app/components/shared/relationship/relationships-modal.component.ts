import {Component, Input, OnInit, OnDestroy, EventEmitter, Output} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {BaseComponent} from '../../shared/base.component';
import {PermissionsService} from '../../../services/permissions.service';
import {ObjectDetailService} from '../../../services/object-detail.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-relationships-modal',
    templateUrl: `./relationships-modal.component.html`,
    providers: [PermissionsService, ObjectDetailService]
})

export class RelationshipsModalComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;

    @Input() objectID: number;
    @Input() objectType: string;
    @Input() isModalVisible: boolean = false;
    @Input() subtitle: string;
    @Output() onClose = new EventEmitter;
    private componentTitle: string = 'Related Assets';
    

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private permissionsService: PermissionsService,
        private objectDetailService: ObjectDetailService,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnDestroy(): void {
        this.cancel();
    }

    ngOnInit() {    
            this.loadPermissions(this.permissionsService, this.objectType, this.objectID);
    }

    cancel() {
        this.isModalVisible = false;
        this.onClose.emit(null);
    }
}
