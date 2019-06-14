import { Input, Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { Title } from '@angular/platform-browser';
import { TreeNode } from 'primeng/primeng';

import { ArtifactTypeService } from '../../services/artifact-type.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ArtifactTypeSummary } from '../../models/artifact-type.model';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { RightSidebarService } from '../../services/right-sidebar.service';

@Component({
    selector: 'd3s-artifact-top-level-list',
    templateUrl: './artifact-top-level-list.component.html',
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

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private artifactsService: ArtifactTypeService,        
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        rightSidebarService: RightSidebarService
    ) {
        super(headerBreadcrumbService);

        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Glossary');
        this.clearSidebar();
        this.load();
    }

    private load() {
        this.isLoading = true;
        this
            .artifactsService
            .getArtifactTypeTree()
            .subscribe(data => {
                for (let i = 0; i < data.length; i++) {
                    if (data[i].data.Description != null)
                    data[i].data.Description = this.htmlDecode(data[i].data.Description);
                }
                this.ArtifactTypes = data;
                this.selectedRow = this.ArtifactTypes[0];

                this.headerBreadcrumbService.clearBreadcrumbs();
                this.headerBreadcrumbService.clearCurrentObjectInfo();
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.folderTitle ? this.folderTitle : this.area));

                this.isLoading = false;
            }
        ); 
    }

    private htmlDecode(val: string): string {
        return val ? String(val).replace(/<[^>]+>/gm, '') : '';
    }

    navigate(item: any) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('ArtifactType', item.ID));
    }
};
