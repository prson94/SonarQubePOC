///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactTypeService, HeaderBreadcrumbService, PageHeader } from '../../services/index';
import { ArtifactType } from '../../models/artifact-type.model';
import { DataTable, Column} from 'primeng/primeng';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-artifact-list',
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <div *ngIf="isLoading">
                            <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        </div>
                        <div class="tile tile-detail" *ngIf="!isLoading">
                            <d3s-artifact-grid [artifactType]="artifactType"></d3s-artifact-grid>                                                                       
                        </div>
                    </div>
                </div>
                `,
    providers: [ArtifactTypeService],
    directives: [ArtifactGridComponent]
})

export class ArtifactListComponent extends ArtifactBaseComponent implements OnInit, OnDestroy {
    private artifactType: ArtifactType;
    private sub: any;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private artifactTypeService: ArtifactTypeService,        
        pageHeader: PageHeader,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title) {
        super(headerBreadcrumbService, pageHeader);                   
    }

    ngOnInit() {
        
        this.sub = this.route.params.subscribe(params => {
            let artifactTypeId = +params['artifactTypeId']; // (+) converts string 'id' to a number
            this.isLoading = true;
            this.artifactTypeService.getArtifactTypeDetails(artifactTypeId)
                .then(artifactType => {
                    this.artifactType = artifactType;
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area));   
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.artifactType.Name, null));
                    this.setBrowserTitle(this.titleService, this.artifactType.Name);
                    this.isLoading = false;
                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

};