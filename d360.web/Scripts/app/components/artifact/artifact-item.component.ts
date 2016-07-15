///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactService, HeaderBreadcrumbService, PageHeader } from '../../services/index';
import { Artifact } from '../../models/artifacts.model';
import { DataTable, Column} from 'primeng/primeng';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';

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
                    this.isLoading = false;
                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }


};