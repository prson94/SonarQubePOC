import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'd3s-mapping-component',
    template: `
        <div class="row">
            <div class="col s10 offset-s1">
                <div class="tile tile-detail">
                    <header>
                        Mappings
                    </header>
                </div>
            </div>
        </div>
         `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class MappingComponent extends BaseComponent implements OnInit {
    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Mapping');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Mappings'));
    }
};