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
import { AssetTypeClass } from '../../models/asset.model';

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
    private sub: any;
    assetTypeClass: AssetTypeClass;
    public searchValue: string;
    
    constructor(        
        private router: Router,
        private route: ActivatedRoute,
        private artifactsService: ArtifactTypeService,        
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        rightSidebarService: RightSidebarService
    ) {
        super(headerBreadcrumbService, rightSidebarService);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            try {
                let assetTypeClassString: keyof typeof AssetTypeClass = params['class'];
                this.assetTypeClass = AssetTypeClass[assetTypeClassString];
                if (!this.assetTypeClass) {
                    this.assetTypeClass = AssetTypeClass.Business;
                }
            } catch (e) {
                this.assetTypeClass = AssetTypeClass.Business;
            }

            switch (this.assetTypeClass) {
                case AssetTypeClass.Business:

                    this.headerBreadcrumbService.getFolderTitle('#Business').then(res => {
                        this.folderTitle = res;
                        this.setBrowserTitle(this.titleService, res);
                        this.area = res;
                    });
                    
                    break;
                case AssetTypeClass.Technical:

                    this.headerBreadcrumbService.getFolderTitle('#Technical').then(res => {
                        this.folderTitle = res;
                        this.setBrowserTitle(this.titleService, res);
                        this.area = res;
                    });

                    break;
                default:
                    let className: string = AssetTypeClass[this.assetTypeClass];
                    this.folderTitle = `${className} Assets`;
                    this.setBrowserTitle(this.titleService, this.folderTitle);
                    this.area = this.folderTitle;
                    break;
            }

            this.load();
        });
    }

    private load() {
        this.isLoading = true;
        this
            .artifactsService
            .getArtifactTypeTree(this.assetTypeClass)
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