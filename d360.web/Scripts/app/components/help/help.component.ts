import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResourcesService } from '../../services/resources.service';
import { HelpResource } from '../../models/resource.model';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Observable } from 'rxjs';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-help-component',
    providers: [HeaderBreadcrumbService, ResourcesService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <div class="row">
            <div class="col s10 offset-s1">
                <div class="tile tile-detail">
                    <header>
                        Help
                    </header>

                    <div>
                        <div class="row" *ngFor="let hr of (helpResources$ | async)">
                            <div class="col s12">
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
    helpResources$: Observable<any>;

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected resourceService: ResourcesService,
        protected settingsService: CompanySettingsService,
        protected changeDetectorRef: ChangeDetectorRef
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Help');

        this.helpResources$ = this.resourceService.getHelpResources();
        
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Help'));
    }
}