///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactService, HeaderBreadcrumbService, PageHeader, RightSidebarService } from '../../services/index';
import { Artifact } from '../../models/artifacts.model';
import { DataTable, Column, Accordion, AccordionTab } from 'primeng/primeng';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { ArtifactDefnintionComponent } from './artifact-definition.component';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { AuditComponent} from '../shared/audit.component';

@Component({
    selector: 'd3s-artifact-item',
    template: `  <div class="row" *ngIf="isLoading">
                    <div class="col s12">
                        <div>
                            <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        </div>
                    </div>
                </div>
                <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="artifact?.ID" [objectName]="artifact?.Name" [objectType]="'Artifact'"></d3s-audit>
                <div *ngIf="!isLoading && !isAuditVisible">
                    <div class="row">
                        <div class="col s12">
                             <div class="tile tile-detail">
                                <!--governance tile-->  
                                governance tile                                                                                  
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <div class="tile tile-detail">
                                <!--<object-detail [objectType]="'Artifact'" [objectID]="artifact?.ID"></object-detail>-->
                                <p-accordion [multiple]="true">
                                    <p-accordionTab header="Definition" [selected]="true">
                                        <d3s-artifact-definition [artifact]="artifact" [showHeader]="false" ></d3s-artifact-definition>  
                                    </p-accordionTab>
                                    <p-accordionTab header="Synonyms">
                                        artifact synonyms
                                    </p-accordionTab>
                                    <p-accordionTab header="Attribute">
                                        artifact attribute
                                    </p-accordionTab>
                                    <p-accordionTab header="Structure">
                                        artifact structure
                                    </p-accordionTab>
                                </p-accordion>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <div class="tile tile-detail">
                                <!-- grid -->
                            </div>
                        </div>
                    </div>
                </div>                
                `,
    directives: [ArtifactDefnintionComponent, ObjectDetailTile, Accordion, AccordionTab, AuditComponent],
    providers: [ArtifactService]
})

export class ArtifactItemComponent extends ArtifactBaseComponent implements OnInit, OnDestroy {
    private artifact: Artifact
    private sub: any;

    constructor(private route: ActivatedRoute,
        rightSidebarService: RightSidebarService,
        private router: Router,
        private artifactService: ArtifactService,
        pageHeader: PageHeader,
        private titleService: Title,
        headerBreadcrumbService: HeaderBreadcrumbService) {
        super(headerBreadcrumbService, pageHeader, rightSidebarService);

        this.setCommonRightSideBar();
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
        this.clearSidebar();
    }


};