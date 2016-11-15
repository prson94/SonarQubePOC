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
                    <li *ngIf="hasRaiseIssueButton"><d3s-raise-issue-button></d3s-raise-issue-button></li>
                    <li *ngIf="headerActionsService.showFavorite && !isAdminUrl" style="cursor: pointer"><d3s-header-favorites [uri]="uri"></d3s-header-favorites></li>
                    <li *ngIf="headerActionsService.showFollow  && !isAdminUrl" style="cursor: pointer"><d3s-header-follow></d3s-header-follow></li>
                    <li *ngIf="headerActionsService.showLegacy"><a href="/legacy" title="Go to legacy UI"><i class="fa fa-moon-o"></i></a></li>
                    <li *ngIf="headerActionsService.showHelp"><a routerLink="help" class="help" title="Get help!"><i class="fa fa-question-circle"></i></a></li>
                    <li *ngIf="headerActionsService.showSearch"><d3s-header-typeahead-search></d3s-header-typeahead-search></li>
                    <li *ngIf="headerActionsService.showNotifications"><a href="#" title="Go to notification settings"><i class="fa fa-bell-o"></i></a></li>
                    <li><a [routerLink]="resourceUrl()" class="photo" title="Go to your profile"><img [src]="'/resources/image/' + resourceId + '?size=25'" height="25" width="25" /></a></li>
                </ul> 
                `,
})

export class HeaderActionsComponent {        
    private resourceId: number = CurrentResourceID;
    private sub;
    private isAdminUrl = false;
    private uri = "";
    private hasRaiseIssueButton: boolean = true;

    constructor(private headerActionsService: HeaderActionsService, private router: Router) { }

    ngOnInit() {
        this.sub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.uri = _.trimStart(e.url,'/');
                this.isAdminUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
            }
            //dont show raise issue button on raise issue screen or any admin screens            
            this.hasRaiseIssueButton = (!e.url.toLowerCase().endsWith('workflow/raiseissue') && (e.url.toLowerCase().indexOf('/admin/') == -1));            
        });
    }

    private resourceUrl() {
        return SiteUrlHelpers.getObjectUrl('Resource', this.resourceId);
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}

