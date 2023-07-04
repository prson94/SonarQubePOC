import { Component, EventEmitter, Input, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { SidePanelButton } from '../../../models/side-panel.model';
import { PopupMenuItem } from '../controls/popup-menu/popup-menu.component';
import { BaseComponent } from '../base.component';
import { StateService } from '../../../services/state.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';
import { FeatureFlags } from "../../../services/feature-flags.enum";
import { SidePanelService } from '../../../services/side-panel.service';

@Component({
    selector: 'side-panel',
    templateUrl: './side-panel.component.html',
    styleUrls: ['./side-panel.component.less'],
    encapsulation: ViewEncapsulation.None
})

export class SidePanelComponent extends BaseComponent {
    @Input() height = 'calc(100vh - 270px)';
    @Input() isSecondarySidePanel: boolean = false
    @Input() hasDetail: boolean = false;
    @Input() hasProfiling: boolean = false;
    @Input() hasFilter: boolean = false;
    @Input() disableProfiling: boolean = false;

    @Input() expanded: boolean = false;
    @Output() expandedChange = new EventEmitter<boolean>();

    @Output() buttonClick = new EventEmitter<string>();

    @Input() panelApplies: boolean = true;
    @Input() selectedItem: any = {};
    @Input() selectedPanel: string = '';
    @Input() showEmptyOverlay: boolean = false;
    @Input() setMinMaxWidth: boolean = true;
    @Output() selectedPanelChange = new EventEmitter<string>();

    @Input() storageKey: string = null;
    readonly storageKeyPrefix: string = 'side_panel_';

    @Input() extraButtons: SidePanelButton[] = [];
    @Input() multipleItemsSelected: boolean = false;
	@Input() selectedItemsCount: number = 0;
	@Input() closeIcon: string = 'fa-arrow-circle-right';

    buttons: SidePanelButton[] = [];

    public panelMenu: PopupMenuItem[] = [];

    readonly minWidth = '400px';
    readonly maxWidth = '400px';

	constructor(
		private sidePanelService: SidePanelService,
		private stateService: StateService,
        protected settingsService: CompanySettingsService,
        private featureFlagService: LaunchDarklyService) {
		super(settingsService);

		this.sidePanelService.sidePanelStateChange$.subscribe((state) => {
			if (state.expanded === true) {
				this.expandSidePanel();
			}
			else if (state.expanded === false) {
				this.collapseSidePanel();
			}
		});
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
        if (changes['hasFilter'] && !changes['hasFilter'].isFirstChange() && changes['hasFilter'].currentValue !== changes['hasFilter'].previousValue) {
            loadButtons = true;
        }
        if (changes['hasDetail'] && !changes['hasDetail'].isFirstChange() && changes['hasDetail'].currentValue !== changes['hasDetail'].previousValue) {
            loadButtons = true;
        }
		if (changes['selectedItemsCount'] && !changes['selectedItemsCount'].isFirstChange() && changes['selectedItemsCount'].currentValue !== changes['selectedItemsCount'].previousValue) {
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
            const stateString = localStorage.getItem(this.storageKeyPrefix + this.storageKey);
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
                        const b = this.buttons.find((b) => b.key === state.selectedPanel);

                        if (b || this.storageKey === "relationship-detail") {
                            this.selectedPanel = state.selectedPanel;
                        }
                    }
                }

            }
            else if (this.selectedPanel === 'filters') {
                this.expanded = true;
            }
        }
    }

    private saveState() {
        if (this.storageKey != null && this.storageKey.length > 0) {
            const state: any = {};
            state.expanded = this.expanded;
            state.selectedPanel = this.selectedPanel;

            localStorage.setItem(this.storageKeyPrefix + this.storageKey, JSON.stringify(state));
        }
    }

    private initButtons() {
        this.buttons = [];

        this.extraButtons.forEach((b) => this.buttons.push(b));

        if (this.hasFilter) {
            this.buttons.push(new SidePanelButton({
                label: $localize`Filters`,
                tooltip: $localize`Filters`,
                disabledTooltip: '',
                nothingSelectedMessage: '',
                notApplicableMessage: '',
                multipleSelectedMessage: '',
                key: 'filters',
                icon: 'fa-filter',
                visible: true
            }));
        }

        if (this.hasDetail) {
            this.buttons.push(new SidePanelButton({
                label: $localize`Information`,
                tooltip: $localize`Information`,
                disabledTooltip: null,
                nothingSelectedMessage: $localize`Select an item from the list to display its properties`,
                notApplicableMessage: $localize`Information data is not available for the selected item`,
                multipleSelectedMessage: $localize`Select a single item to display it’s properties`,
                key: 'detail',
                icon: 'fa-info-circle',
                disabled: false,
                visible: true
            }));
        }

        if (this.hasProfiling && this.featureFlagService.variation<boolean>(FeatureFlags.DataProfilingUiFlag)) {
            this.buttons.push(new SidePanelButton({
                label: $localize`Profiling`,
                tooltip: $localize`Profiling`,
                disabledTooltip: $localize`Profiling data is not available for the selected item`,
                nothingSelectedMessage: $localize`Select an item from the list to display its profiling data`,
                notApplicableMessage: $localize`Profiling data is not available for the selected item`,
                multipleSelectedMessage: $localize`Select a single item to display it’s profiling information`,
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
            const ix = this.buttons.findIndex((b) => b.key === this.selectedPanel);
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
            const ix = this.buttons.findIndex((b) => b.key === this.selectedPanel);
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

	private expandSidePanel() {
		this.expanded = true;
		this.expandedChange.emit(true);
		this.stateService.recalculateTagSize();

		this.saveState();
	}
}