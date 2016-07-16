///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactService, HeaderBreadcrumbService, PageHeader } from '../../services/index';
import { Artifact } from '../../models/artifacts.model';
import { DataTable, Column} from 'primeng/primeng';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-artifact-item',
    template: ` 
                <div [innerHTML]="artifact?.Description"></div>
                `,
    providers: [ArtifactService]
})

export class ArtifactItemComponent extends ArtifactBaseComponent implements OnInit, OnDestroy {
    private artifact: Artifact
    private sub: any;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private artifactService: ArtifactService,
        pageHeader: PageHeader,
        private titleService: Title,
        headerBreadcrumbService: HeaderBreadcrumbService) {
        super(headerBreadcrumbService, pageHeader);
    }

    ngOnInit() {

        this.sub = this.route.params.subscribe(params => {
            //let artifactTypeId = +params['artifactTypeId']; // (+) converts string 'id' to a number
            let artifactId = +params['artifactId']; // (+) converts string 'id' to a number
            this.isLoading = true;
            this.artifactService.getArtifact(artifactId)
                .then(artifact => {
                    this.artifact = artifact;
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    for (let breadcrumb of this.artifact.Breadcrumbs) {
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(breadcrumb.Name, breadcrumb.Url, breadcrumb.Active));
                    }             
                    this.setBrowserTitle(this.titleService, this.artifact.Name);       
                    this.isLoading = false;
                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }


};