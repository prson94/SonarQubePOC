import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { CompanySettingsService } from '../../services/settings.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResourcesService } from '../../services/resources.service';
import { HelpResource } from '../../models/resource.model';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'd3s-help-component',
    providers: [CompanySettingsService, HeaderBreadcrumbService, ResourcesService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <div class="row">
            <div class="col s10 offset-s1">
                <div class="tile tile-detail">
                    <header>
                        Help
                    </header>

                    <div>
                        <div class="row" *ngFor="let hr of helpResources">
                            <div class="col s12 m4 l4" *ngIf="isIFrame(hr)">
                                <h4>{{hr.Name}}</h4>
                                <div class="directions">{{hr.Description}}</div>
                            </div>
                            <div class="col s12 m8 l8" *ngIf="isIFrame(hr)">
                                <iframe [src]="iframeURL(hr)" allowtransparency="true" frameborder="0" scrolling="no" class="wistia_playlist" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width="100%" style="min-height: 360px"></iframe>
                            </div>

                            <div class="col s12" *ngIf="isFile(hr)">
                                <h4><a [href]="hr.Url" target="help">{{hr.Name}}</a></h4>
                                <div class="directions">{{hr.Description}}</div>
                            </div>
                        </div>
                    </div>
                </div>                
            </div>
        </div>`
})

export class HelpComponent extends BaseComponent implements OnInit {

    helpResources: HelpResource[] = [];
    //showDefaultHelpVideos: boolean = false;

    constructor(
        protected titleService: Title,
        protected companySettingsService: CompanySettingsService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected resourceService: ResourcesService,
        protected sanitizer: DomSanitizer,
        protected changeDetectorRef: ChangeDetectorRef
    ) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Help');

        //this.companySettingsService.getSettings().then(res => {
        //    this.showDefaultHelpVideos = res.ShowDefaultHelpVideos;
        //});

        this.resourceService.getHelpResources().subscribe(res => {
            this.helpResources = res;
            this.changeDetectorRef.markForCheck();
        });
        
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Help'));
    }

    iframeURL(hr: HelpResource) {
        return this.sanitizer.bypassSecurityTrustResourceUrl(hr.Url);
    }

    isIFrame(hr: HelpResource) {
        return (hr.Type == 1);
    }

    isFile(hr: HelpResource) {
        return (hr.Type == 2);
    }
};