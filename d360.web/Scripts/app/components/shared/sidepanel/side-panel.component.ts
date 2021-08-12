import { Input, Component, EventEmitter, Output, SimpleChanges } from '@angular/core';
import { SidePanelButton } from '../../../models/side-panel.model';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'side-panel',
    templateUrl: './side-panel.component.html',
    styleUrls: ['./side-panel.component.less']
})

export class SidePanelComponent extends BaseComponent {
    @Input() height = 'calc(100vh - 250px)';

    @Input() hasDetail: boolean = false;
    @Input() hasProfiling: boolean = false;
    @Input() disableProfiling: boolean = false;

    @Input() expanded: boolean = false;
    @Output() expandedChange = new EventEmitter<boolean>();

    @Output() buttonClick = new EventEmitter<string>();

    @Input() panelApplies: boolean = true;
    @Input() selectedItem: any = {};
    @Input() selectedPanel: string = '';
    @Output() selectedPanelChange = new EventEmitter<string>();

    buttons: SidePanelButton[] = [];

    readonly minWidth = '400px';
    readonly maxWidth = '400px';

    ngOnInit() {
        this.initButtons();
    }

    ngOnChanges(changes: SimpleChanges) {
        let loadButtons = false;
        if (changes['hasProfiling'] && !changes['hasProfiling'].isFirstChange() && changes['hasProfiling'].currentValue !== changes['hasProfiling'].previousValue) {
            loadButtons = true;
        }
        if (changes['hasDetail'] && !changes['hasDetail'].isFirstChange() && changes['hasDetail'].currentValue !== changes['hasDetail'].previousValue) {
            loadButtons = true;
        }

        if (loadButtons) {
            this.initButtons();
        }

        if (changes['disableProfiling'] && !changes['disableProfiling'].isFirstChange() && changes['disableProfiling'].currentValue !== changes['disableProfiling'].previousValue) {
            if (this.hasProfiling) {
                this.buttons.find((b) => b.key === 'dataprofile').disabled = this.disableProfiling;
            }
        }
    }

    private initButtons() {
        this.buttons = [];

        if (this.hasDetail) {
            this.buttons.push({
                label: 'Information',
                tooltip: 'Information',
                disabledTooltip: null,
                nothingSelectedMessage: 'Select an item from the list to display its properties',
                notApplicableMessage: 'Select an item from the list to display its properties',
                key: 'detail',
                icon: 'fa-info-circle',
                disabled: false,
                visible: true
            });
        }

        if (this.hasProfiling) {
            this.buttons.push({
                label: 'Profiling',
                tooltip: 'Profiling',
                disabledTooltip: 'Profiling data is not available for the selected item',
                nothingSelectedMessage: 'Select an item from the list to display its profiling data',
                notApplicableMessage: 'Profiling data is not available for the selected item',
                key: 'dataprofile',
                icon: 'fa-bar-chart',
                disabled: this.disableProfiling,
                visible: true
            });
        }

        if (this.buttonCount > 0) {
            this.selectedPanel = this.buttons[0].key;
            this.selectedPanelChange.emit(this.buttons[0].key);
        }
    }

    clickButton(b: SidePanelButton) {
        if (!b.disabled) {
            if (this.selectedPanel !== b.key) {
                this.selectedPanel = b.key;
                this.selectedPanelChange.emit(b.key);
            }
            this.buttonClick.emit(b.key);

            this.expanded = true;
            this.expandedChange.emit(true);
        }
    }

    getButtonTooltip(b: SidePanelButton): string {
        if (b.disabled && b.disabledTooltip != null && b.disabledTooltip.length > 0) {
            return b.disabledTooltip;
        } else {
            return b.tooltip;
        }
    }

    get visibleButtons(): SidePanelButton[] {
        return this.buttons.filter((b) => b.visible === true);
    }

    get buttonCount(): number {
        return this.visibleButtons.length;
    }

    get panelLabel(): string {
        if (this.selectedPanel.length > 0) {
            let ix = this.buttons.findIndex((b) => b.key === this.selectedPanel);
            if (ix > -1) {
                return this.buttons[ix].label;
            } else {
                return '';
            }
        } else {
            return '';
        }
    }

    get panelButton(): SidePanelButton {
        if (this.selectedPanel.length > 0) {
            let ix = this.buttons.findIndex((b) => b.key === this.selectedPanel);
            if (ix > -1) {
                return this.buttons[ix];
            } else {
                return null;
            }
        } else {
            return null;
        }
    }

    collapseSidePanel() {
        this.expanded = false;
        this.expandedChange.emit(false);
    }
}