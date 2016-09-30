
import { Component, Input, OnInit, AfterViewInit, ElementRef, OnDestroy } from '@angular/core';

declare var LineageDiagramWrapper: LineageAdapter;
declare var LineageCloseWindow: LineageWindowAdapter;

@Component({
    selector: 'd3s-lineage',
    templateUrl: 'scripts/app/components/shared/lineage.component.html'
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

    public ngOnDestroy() {
        LineageCloseWindow();
    }


}

interface LineageAdapter {
    (w: any): any;
}

interface LineageWindowAdapter {
    (): any;
}