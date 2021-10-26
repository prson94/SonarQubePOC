import { Input, Component, EventEmitter, Output, SimpleChanges } from '@angular/core';
import { SidePanelButton } from '../../../models/side-panel.model';
import { PopupMenuItem } from '../controls/popup-menu/popup-menu.component';
import { BaseComponent } from '../base.component';
import { StateService } from '../../../services/state.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'side-panel',
    templateUrl: './side-panel.component.html',
    styleUrls: ['./side-panel.component.less']
})

export class SidePanelComponent extends BaseComponent {
    @Input() height = 'calc(100vh - 270px)';

    @Input() hasDetail: boolean = false;
    @Input() hasProfiling: boolean = false;
    @Input() disableProfiling: boolean = false;

    @Input() expanded: boolean = false;
    @Output() expandedChange = new EventEmitter<boolean>();

    @Output() buttonClick = new EventEmitter<string>();

    @Input() panelApplies: boolean = true;
    @Input() selectedItem: any = {};
    @Input() selectedPanel: string = '';
    @Input() showEmptyOverlay: boolean = false;
    @Output() selectedPanelChange = new EventEmitter<string>();

    @Input() storageKey: string = null;
    readonly storageKeyPrefix: string = 'side_panel_';

    @Input() extraButtons: SidePanelButton[] = [];
    @Input() multipleItemsSelected: boolean = false;

    buttons: SidePanelButton[] = [];

    public panelMenu: PopupMenuItem[] = [];

    readonly minWidth = '400px';
    readonly maxWidth = '400px';

    constructor(
        private stateService: StateService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnInit() {
        this.initButtons();

        this.selectedPanelChange.emit(this.selectedPanel);
        this.expandedChange.emit(this.expanded);
    }

    ngOnChanges(changes: SimpleChanges) {
        let loadButtons = false;
        let loadState = false;

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

        if (changes['storageKey'] && changes['storageKey'].isFirstChange() && changes['storageKey'].currentValue !== changes['storageKey'].previousValue) {
            loadState = true;
            
        }

        if (changes['expanded'] && changes['expanded'].isFirstChange() && changes['expanded'].currentValue !== changes['expanded'].previousValue) {
            this.stateService.recalculateTagSize();
        }

        if (loadState || loadButtons) {
            this.loadState();

            this.selectedPanelChange.emit(this.selectedPanel);
            this.expandedChange.emit(this.expanded);
        }


    }

    private loadState() {
        if (this.storageKey != null && this.storageKey.length > 0) {
            let stateString = localStorage.getItem(this.storageKeyPrefix + this.storageKey);
            if (stateString != null && stateString.length > 0) {
                let state;
                try {
                    state = JSON.parse(stateString);
                } catch {
                    console.warn('State for key ' + this.storageKey + ' could not be parsed');
                }

                if (state != null) {
                    if (state.expanded != null) {
                        this.expanded = state.expanded;
                        this.stateService.recalculateTagSize();
                    }

                    if (state.selectedPanel != null && state.selectedPanel.length > 0) {
                        let b = this.buttons.find((b) => b.key === state.selectedPanel);

                        if (b) {
                            this.selectedPanel = state.selectedPanel;

                        }
                    }
                }

            }
        }
    }

    private saveState() {
        if (this.storageKey != null && this.storageKey.length > 0) {
            let state: any = {};
            state.expanded = this.expanded;
            state.selectedPanel = this.selectedPanel;

            localStorage.setItem(this.storageKeyPrefix + this.storageKey, JSON.stringify(state));
        }
    }

    private initButtons() {
        this.buttons = [];

        this.extraButtons.forEach((b) => this.buttons.push(b));

        if (this.hasDetail) {
            this.buttons.push(new SidePanelButton({
                label: 'Information',
                tooltip: 'Information',
                disabledTooltip: null,
                nothingSelectedMessage: 'Select an item from the list to display its properties',
                notApplicableMessage: 'Information data is not available for the selected item',
                multipleSelectedMessage: 'Select a single item to display it’s properties',
                key: 'detail',
                icon: 'fa-info-circle',
                disabled: false,
                visible: true
            }));
        }

        if (this.hasProfiling) {
            this.buttons.push(new SidePanelButton({
                label: 'Profiling',
                tooltip: 'Profiling',
                disabledTooltip: 'Profiling data is not available for the selected item',
                nothingSelectedMessage: 'Select an item from the list to display its profiling data',
                notApplicableMessage: 'Profiling data is not available for the selected item',
                multipleSelectedMessage: 'Select a single item to display it’s profiling information',
                key: 'dataprofile',
                icon: 'fa-bar-chart',
                disabled: this.disableProfiling,
                visible: true
            }));
        }

        if (this.buttonCount > 0) {
            this.selectedPanel = this.selectedPanel ? this.selectedPanel : this.buttons[0].key;
            this.panelMenu = this.buttons[0].panelMenu;
            this.selectedPanelChange.emit(this.buttons[0].key);
        }
    }

    clickButton(b: SidePanelButton) {
        if (!b.disabled) {
            if (this.selectedPanel !== b.key) {
                this.selectedPanel = b.key;
                this.panelMenu = b.panelMenu;
                this.selectedPanelChange.emit(b.key);
            }
            this.buttonClick.emit(b.key);

            this.expanded = true;
            this.expandedChange.emit(true);
            this.stateService.recalculateTagSize();

            this.saveState();
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
        this.stateService.recalculateTagSize();

        this.saveState();

    }
}