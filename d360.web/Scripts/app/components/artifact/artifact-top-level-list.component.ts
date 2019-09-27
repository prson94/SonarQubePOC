import { Input, Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { Title } from '@angular/platform-browser';
import { TreeNode } from 'primeng/api';

import { ArtifactTypeService } from '../../services/artifact-type.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
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

    public searchValue: string;
    
    constructor(        
        private router: Router,
        private artifactsService: ArtifactTypeService,        
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        rightSidebarService: RightSidebarService
    ) {
        super(headerBreadcrumbService, rightSidebarService);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Glossary');
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
                if (this.ArtifactTypes != null && this.ArtifactTypes.length > 0) {
                    this.selectedRow = this.ArtifactTypes[0];
                }

                this.headerBreadcrumbService.clearBreadcrumbs();
                this.headerBreadcrumbService.clearCurrentObjectInfo();
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.folderTitle ? this.folderTitle : this.area));
                this.headerBreadcrumbService.getFolderIcon(this.folderTitle ? this.folderTitle : this.area).then(res => {
                    this.rightSidebarService.clearCurrentObject();
                    this.rightSidebarService.clearItems();
                    this.rightSidebarService.setCurrentArea(this.folderTitle ? this.folderTitle : this.area, res, null);
                });

                this.isLoading = false;
            }
        ); 
    }

    private htmlDecode(val: string): string {
        return val ? String(val).replace(/<[^>]+>/gm, '') : '';
    }

    navigate(id: number) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('ArtifactType', id));
    }
};