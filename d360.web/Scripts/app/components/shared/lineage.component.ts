///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, OnInit, AfterViewInit, ElementRef } from '@angular/core';

declare var LineageDiagramWrapper: LineageAdapter;

@Component({
    selector: 'd3s-lineage',
    templateUrl: 'scripts/app/components/shared/lineage.component.html'
    //template: `
    //        <div id="lineage_diagram"></div>
    //    `
})

export class LineageComponent implements OnInit, AfterViewInit {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;
    @Input() readonly: boolean = true;

    constructor(private myElement: ElementRef) {
        
    }

    public ngOnInit() {

        
    }

    public ngAfterViewInit() {
        //TODO: clean this up after changes to Lineage Diagram in old UI stop
        LineageDiagramWrapper(this.myElement.nativeElement); 
    }


}

interface LineageAdapter {
    (w: any): any;
}