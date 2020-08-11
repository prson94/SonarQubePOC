
import { Component, ChangeDetectionStrategy, Input, OnChanges, SimpleChanges, DoCheck, ChangeDetectorRef, ElementRef, ViewChild } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { SortEvent } from 'primeng/api';

@Component({
    selector: 'd3s-process-diagram-list-view',
    templateUrl: './process-diagram-list-view.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramListViewComponent extends DiagramBaseComponent implements DoCheck {
    @Input() nodeArray: go.ObjectData[] = [];
    @Input() nodeSelection: any;
    @Input() diagram: go.Diagram;

    selected: any[] = [];
    private nodeCount: number = 0;

    @ViewChild('dt', { static: false }) tableEl: any;

    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

    }

    ngDoCheck() {
        if (this.nodeArray) {
            if (this.nodeArray.length != this.nodeCount) {
                this.nodeArrayCountChanged();
                this.nodeCount = this.nodeArray.length;
            }

        }

        if (this.nodeSelection) {
            var arr = this.nodeSelection.toArray();
            this.selected = [];
            arr.forEach(x => {
                if (x.data && x.data.key) {
                    this.selected.push(this.nodeArray.find(node => node.key == x.data.key));
                }
            })
        }
        this.cdRef.detectChanges();
    }

    nodeArrayCountChanged() {
        this.nodeArray = this.nodeArray.sort((a, b) => {
            return +a.StepNo - +b.StepNo;
        })
        this.tableEl.reset();
    }
    onRowSelect($event) {
        var part = this.getPartByKey($event.data.key);
        var selection = this.diagram.selection;

        var selectedParts: go.Part[] = [];
        selection.each(s => selectedParts.push(s));
        selectedParts.push(part);

        this.diagram.selectCollection(selectedParts);
    }
    onRowUnselect($event) {
        var part = this.getPartByKey($event.data.key);
        var selection = this.diagram.selection;

        var selectedParts: go.Part[] = [];
        selection.each(s => {
            if (s != part)
                selectedParts.push(s)
        }
        );

        this.diagram.selectCollection(selectedParts);
    }

    toggleAll($event) {
        if ($event.checked) {
            var selectedParts: go.Part[] = [];
            this.nodeArray.forEach(data => {
                selectedParts.push(this.getPartByKey(data.key));
            })
            this.diagram.selectCollection(selectedParts);
        }
        else {
            this.diagram.clearSelection();
        }
    }
    getPartByKey(key: string): go.Part {
        return this.diagram.findPartForKey(key);
    }

    customSort(event: SortEvent) {
        event.data.sort((data1, data2) => {
            let value1 = data1[event.field];
            let value2 = data2[event.field];
            let result = null;

            if (value1 == null && value2 != null)
                result = -1;
            else if (value1 != null && value2 == null)
                result = 1;
            else if (value1 == null && value2 == null)
                result = 0;
            else if (typeof value1 === 'string' && typeof value2 === 'string' && event.field != 'StepNo')
                result = value1.localeCompare(value2);
            else if (event.field == 'StepNo') {
                console.log(+value1);
                console.log(+value2);
                result = (+value1 < +value2) ? -1 : (+value1 > +value2) ? 1 : 0;

            }
            else
                result = (value1 < value2) ? -1 : (value1 > value2) ? 1 : 0;

            return (event.order * result);
        })

    }
}