
import { Component, Input, OnInit, ChangeDetectionStrategy, AfterViewChecked, OnChanges, SimpleChange, SimpleChanges, ChangeDetectorRef, EventEmitter, Output, HostListener } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';

import { ConnectorLabelService } from '../../../../services/connectorLabel.service';
import { Subscription } from 'rxjs';
import { CompanySettingsService } from '../../../../services/settings.service';
@Component({
    selector: 'd3s-process-diagram-label-editor',
    templateUrl: './process-diagram-label-editor.component.html',
    providers: [ConnectorLabelService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramLabelEditorComponent extends DiagramBaseComponent implements OnChanges {
    @Input() linkData: any;
    @Input() assetUid: any;
    @Output() linkDataChange = new EventEmitter();
    private linkLabel: any;
    private labels: any[] = [];
    private createLabelSub: Subscription;
    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private cdRef: ChangeDetectorRef,
        private connectorLabelService: ConnectorLabelService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
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
        this.linkLabel = this.linkData.label;
        this.cdRef.detectChanges();
    }
    search(event) {
        var q = this.linkLabel ? this.linkLabel : '';
        this.connectorLabelService.getAvailableLabels(q)
            .subscribe(res => {
                this.labels = [];
                res.forEach(x => {
                    this.labels.push(x.Value);
                })
                this.cdRef.detectChanges();
            });

    }

    selected($event) {
        this.linkLabel = $event;
        this.updateConnectorLabelToLink();
    }

    onBlur($event) {
        if ($event && $event.relatedTarget && $event.relatedTarget.className.indexOf('clear-label'))
            return;
        this.updateConnectorLabelToLink();
    }
    onKeyUp($event: KeyboardEvent) {
        if ($event.key == 'Enter') {
            var el = $event.target as HTMLElement;
            setTimeout(() => {
                el.blur();
            }, 50);
        }
        if (this.linkLabel == '')
            this.clearLabel();
    }
    clearLabel() {
        if (this.createLabelSub)
            this.createLabelSub.unsubscribe();

        this.linkLabel = '';
        this.linkDataChange.emit({ label: { uid: null, Value: null }, data: this.linkData });
    }

    updateConnectorLabelToLink() {
        if (!this.linkLabel || this.linkLabel.length > 40)
            return;

        if (this.createLabelSub)
            this.createLabelSub.unsubscribe();
        var currentLinkData = this.linkData;
        this.createLabelSub = this.connectorLabelService.createOrGetLabel(this.linkLabel)
            .subscribe(res => {
                this.linkLabel = res.Value;
                this.linkDataChange.emit({ label: { uid: res.uid, Value: res.Value }, data: currentLinkData });
            });
    }
}