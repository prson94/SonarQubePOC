
import { Component, Input, OnInit, ChangeDetectionStrategy, AfterViewChecked, OnChanges, SimpleChange, SimpleChanges, ChangeDetectorRef, EventEmitter, Output } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { AssetTypeService } from '../../../../services/asset-type.service';
import { EditorField } from '../../../../models/editor-field.model';
import { ProcessService } from '../../../../services/process.service';
@Component({
    selector: 'd3s-process-diagram-label-editor',
    templateUrl: './process-diagram-label-editor.component.html',
    providers: [ProcessService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramLabelEditorComponent extends DiagramBaseComponent implements OnChanges {
    @Input() linkData: any;
    @Output() linkDataChange = new EventEmitter();

    private labels: any[] = [];

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
        this.labels = [];
        this.labels.push(event.query);
    }

    select(event) {
    }
}