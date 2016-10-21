import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { HeaderActionsService } from '../../services/header-actions.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-header-actions',
    template: `
                <ul class="right hide-on-med-and-down">
                    <li *ngIf="headerActionsService.showFavorite && !isAdminUrl" style="cursor: pointer"><d3s-header-favorites [uri]="uri"></d3s-header-favorites></li>
                    <li *ngIf="headerActionsService.showFollow  && !isAdminUrl" style="cursor: pointer"><d3s-header-follow></d3s-header-follow></li>
                    <li *ngIf="headerActionsService.showHelp"><a href="#" class="help"><i class="fa fa-question-circle"></i></a></li>
                    <li *ngIf="headerActionsService.showSearch"><d3s-header-typeahead-search></d3s-header-typeahead-search></li>
                    <li *ngIf="headerActionsService.showNotifications"><a href="#"><i class="fa fa-bell-o"></i></a></li>
                    <li><a [routerLink]="resourceUrl()" class="photo"><img [src]="'/resources/image/' + resourceId + '?size=25'" height="25" width="25" /></a></li>
                </ul> 
                `,
})

export class HeaderActionsComponent {        
    private resourceId: number = CurrentResourceID;
    private sub;
    private isAdminUrl = true;
    private uri = "";

    constructor(private headerActionsService: HeaderActionsService, private router: Router) { }

    ngOnInit() {
        this.sub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.uri = _.trimStart(e.url,'/');
                this.isAdminUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
            }
        });
    }

    private resourceUrl() {
        return SiteUrlHelpers.getObjectUrl('Resource', this.resourceId);
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}

