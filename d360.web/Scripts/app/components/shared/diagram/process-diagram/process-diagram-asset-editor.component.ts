
import { Component, Input, OnInit, ChangeDetectionStrategy, AfterViewChecked, OnChanges, SimpleChange, SimpleChanges, ChangeDetectorRef, EventEmitter, Output } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { AssetTypeService } from '../../../../services/asset-type.service';
@Component({
    selector: 'd3s-process-diagram-asset-editor',
    templateUrl: './process-diagram-asset-editor.component.html',
    providers: [AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramAssedEditorComponent extends DiagramBaseComponent implements OnChanges {
    @Input() nodeData: any;
    @Output() nodeDataChange = new EventEmitter();

    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private cdRef: ChangeDetectorRef,
        private assetTypeService: AssetTypeService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.nodeData && changes.nodeData.currentValue != changes.nodeData.previousValue) {
            if (this.nodeData)
                this.load();
        }
    }

    load() {
        this.cdRef.detectChanges();
        this.cdRef.markForCheck();
    }

    private onModelChange($event) {
        $event.key = this.nodeData.key;

        //for (var propertyName in $event) {
        //    if (propertyName != 'key') {
        //        if ($event[propertyName]) {
        //            if ($event[propertyName].Value) {
        //                var value = $event[propertyName].Value;
        //                delete $event[propertyName];
        //                $event[propertyName] = value;
        //            }

        //            if ($event[propertyName] instanceof Date) {
        //                console.log("Ima date");
        //                let date = new Date($event[propertyName]);

        //                date.setMinutes(date.getMinutes() + date.getTimezoneOffset());
        //                let simpleDate = [this.pad(date.getMonth() + 1), this.pad(date.getDate()), this.pad(date.getFullYear())].join('/');
        //                this.form.value[p] = simpleDate;

        //                var value = $event[propertyName].value;
        //                delete $event[propertyName];
        //                $event[propertyName] = value;
        //            }

        //        }
        //    }
        //}

        this.nodeDataChange.emit($event);
    }

    public pad(s): string { return (s < 10) ? '0' + s : s; }

}