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
                `
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