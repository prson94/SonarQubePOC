
import { Component, ChangeDetectionStrategy, Input, OnChanges, SimpleChanges, DoCheck, ChangeDetectorRef, ElementRef, ViewChild } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';

@Component({
    selector: 'd3s-process-diagram-list-view',
    templateUrl: './process-diagram-list-view.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramListViewComponent extends DiagramBaseComponent implements DoCheck {
    @Input() diagram: go.Diagram;

    selected: any[] = [];

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
        this.cdRef.detectChanges();
    }

    load() {
        console.log(this.diagram.model.nodeDataArray);
    }

}