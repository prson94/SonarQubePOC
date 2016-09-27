
import { Input, Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactTypeService, HeaderBreadcrumbService, PageHeader } from '../../services/index';
import { ArtifactTypeSummary } from '../../models/artifact-type.model';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { TreeNode } from 'primeng/primeng';

@Component({
    selector: 'd3s-artifact-top-level-list',
    template: `                 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div class="tile tile-detail" *ngIf="!isLoading">                            
                        <header>Glossary</header>                              
                        <input type="text" [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;">  
                        <p-treeTable [value]="treeNodeArray | treeSearch: searchValue : 'Name'" selectionMode="single" [(selection)]="selected" styleClass="breadcrumbTree" [style]="{'line-height':'25px'}">
                            <p-column field="Name" header="Name">
                                <template let-item="rowData" pTemplate type="body">
                                    <a (click)="showItem(item)">{{item.data.Name}}</a>
                                </template>
                            </p-column>                                                   
                            <p-column field="Description" header="Description">
                                <template let-item="rowData" pTemplate type="body">
                                    <span [innerHtml]="item.data.Description"></span>
                                </template>
                            </p-column>                            
                            <p-column field="Draft" header="Draft" [style]="{width:'100px'}"></p-column>
                            <p-column field="UnderReview" header="Under Review" [style]="{width:'100px'}"></p-column>
                            <p-column field="Certified" header="Certified" [style]="{width:'100px'}"></p-column>
                            <p-column field="Total" header="Total" [style]="{width:'100px'}"></p-column>
                            <p-column  [style]="{width:'40px'}">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">                                
                                            <d3s-tooltip objectType="ArtifactType" [objectId]="item.data.ID" tooltipType="preview"><a style="cursor:pointer;" (click)="showItem(item)"><i class="fa fa-info"></i></a></d3s-tooltip>                                    
                                        </div>
                                    </template>
                            </p-column>       
                        </p-treeTable>                                   
                    </div>
                </div>
                `,
    providers: [ArtifactTypeService],
})

export class ArtifactTopLevelListComponent extends ArtifactBaseComponent implements OnInit {    
    private searchValue: string;
    private treeNodeArray: TreeNode[] = [];

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

        this.load();
    }


    private load() {
        this.isLoading = true;
        this.artifactTypeService.getTopLevelSummary()
            .then(res => {
                this.treeNodeArray = this.buildTreeNodeArray(res);
                this.isLoading = false;
            });
    }


    private buildTreeNodeArray(artifactTypeSummary: ArtifactTypeSummary[], Parent?: number): TreeNode[] {
        //find the root items then 

        let rootNodes = artifactTypeSummary.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            res.push({
                label: root.Name,
                data: {
                    ID: root.ID,
                    Name: root.Name,
                    Description: root.Description,
                    Certified: root.Certified,
                    Draft: root.Draft,
                    Total: root.Total,
                    UnderReview: root.UnderReview
                },
                children: (this.buildTreeNodeArray(artifactTypeSummary, root.ID)) //recursively find its children
            });
        }

        return res;
    }

    private showItem(item) {
        if (!item.data || !item.data.ID) {
            console.log("ERROR : MISSING ID ON THE SELECTED ROW");
            return;
        }
        this.router.navigateByUrl(`/a/artifact/${item.data.ID}`);
    }
};