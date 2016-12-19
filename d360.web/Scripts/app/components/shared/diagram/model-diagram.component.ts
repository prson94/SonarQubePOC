import { Component, Input, OnInit, AfterViewInit, ElementRef, ViewChild, HostListener, OnDestroy } from '@angular/core';
import { BaseComponent } from '../base.component';
import { DiagramService } from '../../../services/diagram.service';
import { HierarchyDiagramModel } from '../../../models/model.model';
import { MenuItem } from 'primeng/primeng';

import * as go from 'gojs';
import * as _ from 'lodash';

declare var window: any;


@Component({
    selector: 'd3s-model-diagram',
    template: `
<div class="tile tile-detail">
    <header>
        <span>Hierarchy</span>
        <span *ngIf="isLoading" id="LoadingProgress" style="color: #e2792a"><i class="fa fa-refresh fa-spin fa-lg fa-fw"></i>Loading...</span>
        <d3s-tile-actions hasMenu="true" [menuItems]="menuItems" (menuClick)="menuAction($event)" ></d3s-tile-actions>
    </header>
    <div style="position:relative;left: 100%; display: inline; width: 1px;">
        <d3s-overlay-window width="500" maxHeight="400" padding="15" [(visible)]="isWindowVisible" [headerText]="(selectedNode != null) ? headerText : ''">
            <div *ngIf="selectedNode == null">Nothing selected</div>
            <ul class="tab-menu" *ngIf="selectedNode != null">
                <li (click)="selectTab('info')" class="tab-item" [class.selected]="tab == 'info'" *ngIf="selectedNode != null">
                    <i class="fa fa-info-circle fa-2x"></i>
                </li>
                <li (click)="selectTab('user')" class="tab-item" [class.selected]="tab == 'user'" *ngIf="selectedNode != null">
                    <i class="fa fa-user fa-2x"></i>
                </li>
                <li (click)="selectTab('relations')" class="tab-item" [class.selected]="tab == 'relations'" *ngIf="selectedNode != null">
                    <i class="fa fa-retweet fa-2x"></i>
                </li>
            </ul>
            <div [ngSwitch]="tab">
                <div *ngSwitchCase="'info'">
                    <d3s-lineage-object-detail *ngIf="selectedNode != null" [objectType]="(selectedNode.key == 0) ? 'TaxonomyType' : 'Taxonomy'" [objectId]="(selectedNode.key == 0) ? id : selectedNode.key"></d3s-lineage-object-detail>
                </div>
                <div *ngSwitchCase="'user'">
                    <d3s-lineage-responsibilities *ngIf="selectedNode != null" objectType="Taxonomy" [objectId]="selectedNode.key"></d3s-lineage-responsibilities>
                </div>
                <div *ngSwitchCase="'relations'">
                    <d3s-lineage-relations *ngIf="selectedNode != null" objectType="Taxonomy" [objectId]="selectedNode.key"></d3s-lineage-relations>
                </div>
            </div>
        </d3s-overlay-window>
    </div>

    <div id="HierarchyDiagram" style="overflow: hidden;" class="diagram" #diagram></div>

</div>
`,
    providers: [DiagramService]
})

export class ModelDiagramComponent extends BaseComponent implements OnInit, AfterViewInit, OnDestroy {
    @Input() id: number = 0;
    @ViewChild('diagram') diagramRef;



    private g = go.GraphObject.make;
    private myDiagram: go.Diagram;

    private items: HierarchyDiagramModel[] = [];
    private selectedNode: any = null;

    private menuItems: MenuItem[] = [];
    private zoomLevel: number = 50;
    private isWindowVisible = false;
    private headerText = 'Info';
    private tab = 'info';


    constructor(private myElement: ElementRef, private diagramService: DiagramService) {
        super();
    }

    public ngOnInit() {
        this.menuItems.push({
            icon: 'fa-refresh menu-icon'
        });
        this.menuItems.push({
            icon: 'fa-info-circle menu-icon'
        });

        this.initializeDiagram();
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    public ngOnDestroy() {
        //garbage collection
        this.myDiagram.div = null;
    }

    private initializeDiagram() {
        this.myDiagram = this.createDiagram();

        this.myDiagram.nodeTemplate = this.createNodeTemplate();
        this.myDiagram.linkTemplate = this.createLinkTemplate();

        this.myDiagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));
        this.myDiagram.addDiagramListener('ViewPortBoundsChanged', () => this.ViewPortBoundsChanged());

        this.populateDiagram();

    }

    private populateDiagram() {
        this.isLoading = true;
        this.diagramService.getCatalogDiagram(this.id)
            .then(data => {
                this.items = data;
                delete this.items[0].parent;

                this.myDiagram.model = new go.TreeModel(this.items);
                this.isLoading = false;
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

    private ViewPortBoundsChanged() {
        var s = this.myDiagram.scale;
        var h = 500;
        if (s > 1) {
            h = h * s;
        }
        this.zoomLevel = _.clamp(_.round(this.myDiagram.scale * 75), 0, 100);
    }

    private ChangedSelection(e: any) {

        let node = e.diagram.selection.first();

        if (node == null) {
            this.selectedNode = null;
            return;
        }

        this.selectedNode = node.data;
    }
    

    private menuAction(e: MenuItem) {
        if (e.icon == 'fa-refresh menu-icon') {
            this.populateDiagram();
        } else if (e.icon == 'fa-info-circle menu-icon') {
            this.isWindowVisible = !this.isWindowVisible;
        }

    }

    private selectTab(val: string) {
        switch (val) {
            case 'info': this.headerText = 'Info'; break;
            case 'user': this.headerText = 'Responsibilities'; break;
            case 'relations': this.headerText = 'Relationships'; break;
            default: this.headerText = ''; break;
        }
        this.tab = val;
    }

    //#endregion

    //#region templates

    private createDiagram(): go.Diagram {
        return this.g(go.Diagram,
            "HierarchyDiagram",
            { allowCopy: false, layout: this.g(go.TreeLayout, { angle: 90, nodeSpacing: 10, layerSpacing: 40, layerStyle: go.TreeLayout.LayerUniform }) }
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
                { font: "bold 8pt Helvetica, bold Arial, sans-serif", textAlign: "center", margin: 6, maxSize: new go.Size(90, NaN) },
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

    //#endregion
}


