import { Component, ChangeDetectionStrategy, Input, OnChanges, SimpleChanges, ChangeDetectorRef, Output, EventEmitter } from '@angular/core';
import { ConnectorLabelService } from '../../../services/connectorLabel.service';

@Component({
    selector: 'd3s-where-used',
    templateUrl: './where-used.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ConnectorLabelService]
})

export class WhereUsedComponent implements OnChanges {
    @Input() uid: string = '';
    @Input() objectType: string = '';
    @Input() showAsTable: boolean = true;
    @Input() showAsModal: boolean = false;
    @Input() displayValue: string = '';

    @Output() onLoaded = new EventEmitter<any>();

    private usage: any[] = [];
    private isUsageLoading: boolean = false;
    private hasUsage: boolean = false;
    private isModalVisible: boolean = false;
    constructor(
        private connectorLabelService: ConnectorLabelService,
        private cdRef: ChangeDetectorRef
    ) {

    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes && changes.uid.currentValue != changes.uid.previousValue) {
            this.load();
        }
    }
    getFriendlyObjectType(): string {
        if (this.objectType == "ConnectorLabel") {
            return "label";
        }
        return "";
    }

    load() {
        if (this.objectType == "ConnectorLabel") {
            this.loadConnectorLabelUsage();
        }
    }
    export() {
        if (this.objectType == "ConnectorLabel") {
            this.connectorLabelService.exportLabelUsage(this.uid, `Where Used report for Connector Label "${this.displayValue}"`)
        }
    }

    loadConnectorLabelUsage() {
        this.isUsageLoading = true;
        this.hasUsage = false;
        this.connectorLabelService.getLabelUsage(this.uid)
            .subscribe(res => {
                this.usage = res;
                if (this.usage.length > 0) {
                    this.hasUsage = true;
                }
                this.isUsageLoading = false;
                this.onLoaded.emit(this.usage);
                this.cdRef.markForCheck();
            });
    }

    openUsage() {
        if (this.hasUsage) {
            this.isModalVisible = true
        }
    }

}
