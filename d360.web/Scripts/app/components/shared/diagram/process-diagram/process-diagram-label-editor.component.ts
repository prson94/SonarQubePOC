
import { Component, Input, OnInit, ChangeDetectionStrategy, AfterViewChecked, OnChanges, SimpleChange, SimpleChanges, ChangeDetectorRef, EventEmitter, Output, HostListener } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';

import { ProcessService } from '../../../../services/process.service';
@Component({
    selector: 'd3s-process-diagram-label-editor',
    templateUrl: './process-diagram-label-editor.component.html',
    providers: [ProcessService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramLabelEditorComponent extends DiagramBaseComponent implements OnChanges {
    @Input() linkData: any;
    @Input() assetUid: any;
    @Output() linkDataChange = new EventEmitter();

    private labels: any[] = ['tests', 'make one new'];

    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private cdRef: ChangeDetectorRef,
        private processService: ProcessService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.linkData && changes.linkData.currentValue != changes.linkData.previousValue) {
            if (this.linkData)
                this.load();
        }
    }

    load() {
        this.cdRef.detectChanges();
        this.cdRef.markForCheck();
    }
    search(event) {

        this.processService.getAvailableLabels(this.assetUid)
            .subscribe(res => {
                this.labels = [];
                this.labels.push(event.query);
                res.forEach(x => {
                    this.labels.push(x);
                })
            });

    }

    selected($event) {
        this.linkDataChange.emit({ label: $event, data: this.linkData });
    }
}