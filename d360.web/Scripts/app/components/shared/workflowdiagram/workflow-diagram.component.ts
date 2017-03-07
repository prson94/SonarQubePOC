import { Component, Input, OnInit, AfterViewInit, ElementRef, OnDestroy, ViewChild, Renderer, HostListener } from '@angular/core';
import { PermissionsService } from '../../../services/permissions.service';
import { BaseComponent } from '../base.component';

import * as go from 'gojs';
import * as _ from 'lodash';

declare var window: any;

@Component({
    selector: 'd3s-workflow-diagram',
    templateUrl: './workflow-diagram.component.html',
    providers: []
})

export class WorkflowDiagramComponent extends BaseComponent implements OnInit, AfterViewInit {
    @Input() id: number = 0;
    @Input() readonly: boolean = true;
    @ViewChild('workflowDiagram') diagramRef;



    //diagram properties
    private g = go.GraphObject.make;
    private myDiagram: go.Diagram;

    constructor(private myElement: ElementRef, protected permissionsService: PermissionsService, private renderer: Renderer) {
        super();
    }

    public ngOnInit() {
        this.initializeDiagram();

    }

    public ngAfterViewInit() {
        this.resizeDiagram();
    }

    public ngOnDestroy() {
        //garbage collection
        this.myDiagram.div = null;
    }

    //#region helper methods


    private unsubscribe() {

    }


    private initializeDiagram() {
        //this.myDiagram = this.createDiagram();
      

        this.myDiagram.grid.visible = false;
        this.myDiagram.grid.gridCellSize = new go.Size(8, 8);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.populateDiagram();
    }

    private populateDiagram(): Promise<any> {
        this.isLoading = true;
        return null;      
    }


    //#endregion

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

    //#endregion

    //#region templates

    //#endregion
}

