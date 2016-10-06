
import { Component, Input, OnInit, AfterViewInit, ElementRef } from '@angular/core';

declare var ImpactDiagramWrapper: ImpactAdapter;

@Component({
    selector: 'd3s-impact',
    templateUrl: './impact.component.html'
})

export class ImpactComponent implements OnInit, AfterViewInit {
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
        ImpactDiagramWrapper(this.myElement.nativeElement, this.objectType, this.objectID);
    }


}

interface ImpactAdapter {
    (w: any, o: any, oid: any): any;
}