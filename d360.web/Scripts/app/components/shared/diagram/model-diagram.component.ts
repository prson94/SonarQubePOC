import * as go from 'gojs';
import * as _ from 'lodash';
import { AfterViewInit, Component, ElementRef, HostListener, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { HierarchyDiagramModel, Model } from '../../../models/model.model';
import { DiagramBaseComponent } from './diagram-base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { ModelsService } from '../../../services/models.service';
import { Title } from '@angular/platform-browser';
import { SecondaryNavItem, SecondaryNavCurrentObject } from '../../../models/secondaryNav.model';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AssetTypeClass } from '../../../models/asset.model';

declare var window: any;
declare var CurrentResourceID;

@Component({
    selector: 'd3s-model-diagram',
    templateUrl: './model-diagram.component.html',
    providers: [ModelsService]
})

export class ModelDiagramComponent extends DiagramBaseComponent implements OnInit, AfterViewInit, OnDestroy {
    @Input() id: number = 0;
    @ViewChild('diagram', { static: false }) diagramRef;

    private items: HierarchyDiagramModel[] = [];
    public selectedNode: any = null;

    public menuItems: MenuItem[] = [];
    public zoomLevel: number = 50;
    public isWindowVisible = false;
    public headerText = 'Info';
    public tab = 'info';

    public objectType: string = 'TaxonomyType';
    public navFolderName: string = '#Models';
    currentAreaName: any;
    assetType: Model;

    sidePanelOpen: boolean = false;
    sidePanelStorageKey: string = '';
    constructor(
        private myElement: ElementRef,
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private modelsService: ModelsService,
        protected titleService: Title,
    ) {
        super();
        this.breadcrumbsService = headerBreadcrumbService;
        this.secondaryNavService = secondaryNavService;
    }

    public ngOnInit() {
        this.menuItems.push(
            { icon: 'fa fa-refresh menu-icon' },
            { icon: 'fa fa-info-circle menu-icon' }
        );

        this.initializeDiagram();
        this.sidePanelStorageKey = 'detail_' + AssetTypeClass.Model + '_' + CurrentResourceID;
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    public ngOnDestroy() {
        //garbage collection
        this.diagram.div = null;
    }

    private initializeDiagram() {
        this.diagram = this.createDiagram();

        this.diagram.nodeTemplate = this.createNodeTemplate();
        this.diagram.linkTemplate = this.createLinkTemplate();

        this.diagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));
        this.diagram.addDiagramListener('ViewportBoundsChanged', () => this.ViewportBoundsChanged());

        this.populateDiagram();
    }

    private populateDiagram() {
        this.isLoading = true;
        this.modelsService.getCatalogDiagram(this.id).subscribe(
            data => {
                let root = data.find(x => x.parent === null);
                if (root) {
                    delete root.parent;
                }

                this.items = data;

                this.diagram.model = new go.TreeModel(this.items);
                this.isLoading = false;
            }
        );

        this.modelsService.getModel(this.id)
            .subscribe(result => {
                this.assetType = result;
                this.buildNav();
            });

    }

    private htmlDecode(s: string): string {
        s = s.replace(/&#39;/g, '\'');
        s = s.replace(/&amp;/g, '&')
        s = s.replace(/&lt;/g, '<')
        s = s.replace(/&gt;/g, '>')
        s = s.replace(/&#34;/g, '"');

        return s;
    }

    //#region events

    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.resizeDiagram();
    }

    private resizeDiagram() {
        //set the diagram div to a specific height
        //required for GoJS

        let offset = this.diagramRef.nativeElement.offsetTop;
        let height = window.innerHeight;

        if (this.diagramRef.nativeElement.offsetParent) {
            offset += this.diagramRef.nativeElement.offsetParent.offsetTop;
        }

        this.diagramRef.nativeElement.style.height = (height - offset - 50) + 'px';
    }

    private ViewportBoundsChanged() {
        var s = this.diagram.scale;
        var h = 500;
        if (s > 1) {
            h = h * s;
        }

        this.zoomLevel = _.clamp(_.round(this.diagram.scale * 75), 0, 100);
    }

    private ChangedSelection(e: any) {
        let node = e.diagram.selection.first();

        if (node == null) {
            this.selectedNode = null;
            return;
        }

        this.selectedNode = node.data;
    }

    public menuAction(e: MenuItem) {
        if (e.icon == 'fa fa-refresh menu-icon') {
            this.populateDiagram();
        } else if (e.icon == 'fa fa-info-circle menu-icon') {
            this.isWindowVisible = !this.isWindowVisible;
        }
    }

    private selectTab(val: string) {
        switch (val) {
            case 'info':
                this.headerText = 'Info';
                break;
            case 'user':
                this.headerText = 'Responsibilities';
                break;
            case 'relations':
                this.headerText = 'Relationships';
                break;
            default:
                this.headerText = '';
                break;
        }

        this.tab = val;
    }

    //#endregion

    //#region templates

    private createDiagram(): go.Diagram {
        return this.g(go.Diagram,
            "HierarchyDiagram",
            {
                allowCopy: false,
                layout: this.g(go.TreeLayout, {
                    angle: 90,
                    nodeSpacing: 10,
                    layerSpacing: 40,
                    layerStyle: go.TreeLayout.LayerUniform
                })
            }
        );
    }

    private createNodeTemplate(): go.Node {
        return this.g(go.Node, "Auto",
            { deletable: false },
            new go.Binding("text", "name"),
            this.g(go.Shape, "Rectangle",
                { fill: "lightgray", stroke: "black", stretch: go.GraphObject.Fill, alignment: go.Spot.Center }
            ),
            this.g(go.TextBlock,
                {
                    font: "bold 8pt Helvetica, bold Arial, sans-serif",
                    textAlign: "center",
                    margin: 6,
                    maxSize: new go.Size(90, NaN)
                },
                new go.Binding("text", "name")
            )
        );
    }

    private createLinkTemplate(): go.Link {
        return this.g(go.Link,
            { routing: go.Link.Orthogonal, corner: 5, selectable: false },
            this.g(go.Shape)
        );
    }

    buildNav() {
        this.setCommonSecondaryNavTabs(true);
        this.breadcrumbsService
            .getAreaName(this.objectType, this.id)
            .subscribe(result => {
                this.currentAreaName = result
                this.breadcrumbsService.getFolderTitle(this.navFolderName).then((res) => {
                    this.breadcrumbsService.clearBreadcrumbs();
                    this.breadcrumbsService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : res, `TaxonomyType/${SiteUrlHelpers.SITE_URL_HIERARCHY_CLASSIFICATION}`));
                    this.breadcrumbsService.showBreadcrumb(new Breadcrumb(this.assetType.Name, SiteUrlHelpers.getObjectUrl(this.objectType, this.assetType.ID), undefined, this.objectType, this.assetType.ID, undefined, undefined, true));

                    this.breadcrumbsService.getAssetFolderIcon(this.objectType, this.id, this.currentAreaName ? this.currentAreaName : res)
                        .subscribe(icon => {
                            this.secondaryNavService.setCurrentArea(this.assetType.Name, icon, "Model");
                            this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject(this.objectType, this.assetType.ID, this.assetType.Name, null, true, null, this.assetType.AssetTypeUID));
                            this.setCommonSecondaryNavTabs(true, false, this.assetType.HasDashboards);
                            let diagramTab = new SecondaryNavItem('Diagram', 'modeldiagram', ['fa-sitemap'], `/sidebar/visualization/diagram/${this.id}`, null, 7)
                            this.secondaryNavService.showItem(diagramTab);
                            diagramTab.active = true;

                            if (this.auditSidebar) {
                                this.auditSidebar.url = `/sidebar/audit/${this.assetType.AssetTypeUID}`;
                            }

                            this.secondaryNavService.setLocalHomeUrl(SiteUrlHelpers.getObjectUrl(this.objectType, this.assetType.ID));
                            this.secondaryNavService.showHeader(true);
                        });

                    this.setBrowserTitle(this.titleService, this.assetType.Name);
                    this.isLoading = false;
                });
            });
    }

    //#endregion
}
