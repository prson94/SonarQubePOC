///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactTypeService, HeaderBreadcrumbService, PageHeader } from '../../services/index';
import { ArtifactType } from '../../models/artifact-type.model';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';


@Component({
    selector: 'd3s-artifact-top-level-list',
    template: `                 
                <div class="row">
                    <div class="col s12">
                        <div *ngIf="isLoading">
                            <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        </div>
                        Top Level Glossary Counts goes here
                    </div>
                </div>
                `,
    providers: [ArtifactTypeService],
})

export class ArtifactTopLevelListComponent extends ArtifactBaseComponent implements OnInit {    
    
    constructor(private route: ActivatedRoute,
        private router: Router,
        private artifactTypeService: ArtifactTypeService,
        pageHeader: PageHeader,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title) {
        super(headerBreadcrumbService, pageHeader);


    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Glossary');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Glossary'));
    }
    
};