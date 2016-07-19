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
import { ArtifactDefnintionComponent } from './artifact-definition.component';

@Component({
    selector: 'd3s-artifact-item',
    template: `  <div class="row">
                    <div class="col s12">
                        <div *ngIf="isLoading">
                            <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        </div>
                        <div class="tile tile-detail" *ngIf="!isLoading">
                            <d3s-artifact-definition [artifact]="artifact"></d3s-artifact-definition>                                                                                     
                        </div>
                    </div>
                </div>                
                `,
    directives: [ArtifactDefnintionComponent],
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