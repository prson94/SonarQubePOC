///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, ResourcesService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Router, ActivatedRoute } from '@angular/router';
import { Resource } from '../../models/resource.model';


//TODO: find out where this comes from
declare var CurrentResourceID;

@Component({
    selector: 'd3s-policy-item',
    templateUrl: 'scripts/app/components/resource/resource-item.component.html',
    providers: [ ResourcesService ]
})

export class ResourceItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private resourceId = -1;
    private resource: Resource;
    private isMe = false;

    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService, private route: ActivatedRoute, private resourcesService: ResourcesService) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;
        this.sub = this.route.params.subscribe(params => {
            let resourceId = +params['resourceId'];
            this.resourceId = resourceId;

            this.resourcesService.getResource(this.resourceId)
                .then(r => {
                    this.resource = r;

                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Resource'));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.resource.FirstName} ${this.resource.LastName}`));

                    this.setBrowserTitle(this.titleService, `${this.resource.FirstName} ${this.resource.LastName}`);
                    this.isLoading = false;
                });

            if (this.resourceId.toString() == CurrentResourceID) {
                this.isMe = true;
            }
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
};