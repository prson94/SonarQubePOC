import { Component, OnInit } from '@angular/core';
import { AdminBaseComponent } from '../admin-base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SiteCustomizationsService } from '../../../services/site-customizations.service';
import { Title } from '@angular/platform-browser';
import 'codemirror/mode/css/css.js';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';

@Component({
    selector: 'd3s-admin-customizations-component',
    providers: [SiteCustomizationsService],
    template: `<div class="row">
                    <form (ngSubmit)="saveCustomizations()" #customForm="ngForm">
                        <div class="col s12">                    
                            <div class="tile tile-detail">
                                <header>Style Customizations</header>  
                                <div class="ig-message-box">
                                    <div class="fa fa-exclamation-triangle"></div>
                                    <div class="message-box-text">You can write CSS overrides to customize the branding of Govern. It is strongly recommended that you use a resource skilled in CSS development (ie. a front-end developer) to create and maintain your CSS overrides. You should write the minimal number of rules and CSS properties necessary to achieve your desired results.
<br>We release software updates every month, and these changes could potentially break your CSS overrides. In some cases, your CSS overrides could break updates or new features we add to our product UI. If you use a lot of CSS overrides, you will need to perform UI regression testing every time we release to your development/UAT environment. Because of this, we strongly recommend that you reach out to Infogix to raise enhancement requests, instead of using CSS to remove or alter features or screen layouts.<ng-content></ng-content></div>
                                </div>
                                 <div class="col s12">&nbsp;</div>
                                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                                <div class="row" *ngIf="!isLoading">
                                    <div class="col s12">
                                        <codemirror [(ngModel)]="customCss"
                                                            name="css"
                                                            [config]="baseConfig"
                                                            style="height:600px;">
                                        </codemirror>                                          
                                    </div>
                                    <div class="col s12">&nbsp;</div>
                                    <div class="col s12">
                                        <button pButton type="submit" label="Save"></button>
                                    </div>                    
                                </div>
                            </div>
                        </div>                        
                    </form>
                </div>
                `,
    styleUrls: ['admin-customizations.component.less']
})

export class AdminCustomizationsComponent extends AdminBaseComponent implements OnInit {
    private customCss: string;

    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,        
        titleService: Title,
        secondaryNavService: SecondaryNavService,
        protected siteCustomizationsService: SiteCustomizationsService,
        protected messagesService: MessagesObservableService
    ) {

        super(headerBreadcrumbService, titleService, secondaryNavService);
        this.areaName = StringConstants.Section_Branding;
        this.setCommonItems();
        
    }

    ngOnInit() {
        this.load();
    }

    private baseConfig = {
        lineNumbers: true,       
        theme: 'eclipse',
        mode: 'css'
    };

    private load() {
        this.isLoading = true;
        this.siteCustomizationsService.getCustomCss().subscribe(res => {
            this.isLoading = false;
            this.customCss = res;
        });
    }

    saveCustomizations() {
        this.siteCustomizationsService.saveCustomCss(this.customCss).subscribe(res => {
            this.showMessageForResult(this.messagesService, res);
            window.location.reload();
        });
    }
}