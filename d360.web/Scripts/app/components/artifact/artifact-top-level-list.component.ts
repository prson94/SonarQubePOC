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
                            <ng-template pTemplate="header">
	                            <tr>
		                            <th style="width: 20%">Name</th>
		                            <th style="width: 70%">Description</th>
		                            <th style="width: 5%">Item Count</th>
		                            <th style="width: 5%">Select Item</th>
	                            </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-rowNode let-item="rowData">
	                            <tr [ttSelectableRow]="rowNode">
		                            <td>
			                            <d3s-treeTableToggler [rowNode]="rowNode"></d3s-treeTableToggler>
			                            {{item.Name}}
		                            </td>
                                    <td *ngIf="item.Description;else other_content" [innerHtml]="item.Description"></td>
                                    <ng-template #other_content><td>&nbsp;</td></ng-template>
                                    <td style="overflow: auto; padding-left: 15px; text-align: center">
                                        {{item.kount}}
                                    </td>
                                    <td style="overflow: auto; padding-left: 15px; text-align: center">
                                        <d3s-preview-tooltip objectType="ArtifactType" [objectId]="item.ID" icon="info" (click)="navigate(item)"></d3s-preview-tooltip>
                                    </td>
	                            </tr>
                            </ng-template>
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