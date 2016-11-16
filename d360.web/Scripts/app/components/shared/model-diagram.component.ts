import { Component, Input, OnInit, AfterViewInit, ElementRef, ViewChild, HostListener } from '@angular/core';
import { BaseComponent } from './base.component';
import { DiagramService } from '../../services/index';

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
    </header>

    <div id="HierarchyDiagram" style="overflow: hidden;" class="diagram" #diagram></div>

</div>
`,
    providers: [DiagramService]
})

export class ModelDiagramComponent extends BaseComponent implements OnInit, AfterViewInit {
    @Input() id: number = 0;
    @ViewChild('diagram') diagramRef;



    private g = go.GraphObject.make;
    private myDiagram: go.Diagram;

    private zoomLevel: number = 50;


    constructor(private myElement: ElementRef, private diagramService: DiagramService) {
        super();
    }

    public ngOnInit() {
        this.initializeDiagram();
    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    private initializeDiagram() {
        this.myDiagram = this.createDiagram();

    }

    private populateDiagram() {
        this.isLoading = true;
        this.diagramService.getCatalogDiagram(this.id)
            .then(data => {
                this.isLoading = false;
                console.log(data);
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
        //console.log(offset, height);
        this.diagramRef.nativeElement.style.height = (height - offset - 50) + 'px';
    }

    private ViewPortBoundsChanged() {
        var s = this.myDiagram.scale;
        var h = 500;
        if (s > 1) {
            h = h * s;
        }
        this.zoomLevel = _.clamp(_.round(this.myDiagram.scale * 75), 0, 100);
        //$('#LineageZoomSlider').val(Math.round(myDiagram.scale * 1500));
    }

    private ChangedSelection(e: any) {

    }

    //#endregion

    //#region templates

    private createDiagram(): go.Diagram {
        return null;
    }

    //#endregion
}
