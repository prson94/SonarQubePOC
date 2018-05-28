import { Input, Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactTypeService } from '../../services/artifact-type.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ArtifactTypeSummary } from '../../models/artifact-type.model';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { TreeNode } from 'primeng/primeng';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { RightSidebarService } from '../../services/right-sidebar.service';

@Component({
    selector: 'd3s-artifact-top-level-list',
    template: `           

                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div class="tile tile-detail" *ngIf="!isLoading">                            
                        <header>Glossary</header>                              
                        <input type="text" [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;margin-bottom:10px;">  
                        <p-treeTable [value]="ArtifactTypes | treeSearch: searchValue:'Name'" selectionMode="single" [(selection)]="selectedRow" (selectionChange)="navigate($event.data)" [style]="{ 'width': '100%' }">
                            <p-column field="Name" header="Name" [style]="{ 'width': '20%' }"></p-column>
                            <p-column field="Description" header="Description" [style]="{ 'width': '70%' }"></p-column>
                            <p-column field="kount" header="Item Count" [style]="{ 'width': '5%','overflow':'automatic', 'padding-left':'15px', 'text-align':'center' }"></p-column>
                            <p-column [style]="{ 'width': '5%','overflow':'automatic', 'padding-left':'15px', 'text-align':'center' }" header="Select Item">
                                <ng-template let-col let-item="rowData" pTemplate="body">
                                    <d3s-preview-tooltip objectType="ArtifactType" [objectId]="item.data.ID" icon="info" (click)="navigate(item.data)"></d3s-preview-tooltip>
                                </ng-template>
                            </p-column>
                        </p-treeTable>
                    </div>
                </div>
                `,
    providers: [ArtifactTypeService],
})

export class ArtifactTopLevelListComponent extends ArtifactBaseComponent implements OnInit {   
    searchFilter: string = "";
    objectType: string = "ArtifactType";
    adminType: string = "Artifacts";
    selectedRow: TreeNode;
    ArtifactTypes: TreeNode[];

    private searchValue: string;
    private treeNodeArray: TreeNode[] = [];

    constructor(private route: ActivatedRoute,
        private router: Router,
        private artifactsService: ArtifactTypeService,        
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title, rightSidebarService: RightSidebarService) {

        super(headerBreadcrumbService);
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Glossary');
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Glossary'));
        this.clearSidebar();
        this.load();
    }


    private load() {
        this.isLoading = true;
        this.artifactsService.getArtifactTypeTree()
            .then(data => {
                for (let i = 0; i < data.length; i++) {
                    if (data[i].data.Description != null)
                    data[i].data.Description = this.htmlDecode(data[i].data.Description);
                }
                this.ArtifactTypes = data;
                this.selectedRow = this.ArtifactTypes[0];

                this.isLoading = false;
            }); 
    }

    private htmlDecode(val: string): string {
        return val ? String(val).replace(/<[^>]+>/gm, '') : '';
    }

    navigate(item: any) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('ArtifactType', item.ID));
    }
};