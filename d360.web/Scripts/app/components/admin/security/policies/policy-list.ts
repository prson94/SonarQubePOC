import { ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, Output, QueryList, SimpleChange, ViewChild, ViewChildren, ViewEncapsulation } from '@angular/core';
import { Table } from "primeng/table";
import { ReadSecurityPolicy } from '../../../../models/security.model';
import { MessagesObservableService } from '../../../../services/messages-observable.service';
import { SecurityService } from '../../../../services/security.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { SidePanelService } from '../../../../services/side-panel.service';
import { AppConstants } from '../../../../static/constants';
import { BaseComponent } from '../../../shared/base.component';
import { PopupMenu } from '../../../shared/controls/popup-menu/popup-menu.component';

@Component({
	selector: 'policy-list',
	providers: [SecurityService],
	templateUrl: 'policy-list.html',
	styleUrls: ['policy-list.less'],
	encapsulation: ViewEncapsulation.None
})

export class PolicyList extends BaseComponent implements OnChanges {
	items: ReadSecurityPolicy[] = [];

	rowsPerPage: number = AppConstants.DEFAULT_ROWS_PER_PAGE;
	defaultPagingOptions = AppConstants.DEFAULT_PAGING_OPTIONS;

	@Input() filterToName: string = "";

	selected: ReadSecurityPolicy;
	@Output() selectedChange = new EventEmitter();

	first: number = 0;

	showEditor: boolean = false;
	showDelete: boolean = false;
	gridStorageKey: string = "policy-grid";
	simpleFilterValue: string = "";

	editorSelectedUid: string = "";

	/*global $localize*/

	@ViewChild('dt', { static: false }) dataTable: Table;

	constructor(
		private messagesService: MessagesObservableService,
		private securityService: SecurityService,
		protected settingsService: CompanySettingsService,
		private sidePanelService: SidePanelService,
		private cdRef: ChangeDetectorRef
	) {
		super(settingsService);
		this.filterToName = '';

		this.sidePanelService.editClickSource$.subscribe((res) => {
			const data = res as ReadSecurityPolicy;
			//this.edit(RelationshipType.ConvertToUIModeldata(data, this.featureFlagService.variation<boolean>(FeatureFlags.RelationshipCardinalityTempFlag)));
		});
	}

	ngOnInit() {
		this.getData();
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if ( changes['filterToName'] && changes['filterToName'].currentValue !== changes['filterToName'].previousValue ) {
			this.getData();
		}
	}

	getData() {
		this.isLoading = true;
		let obs = this.securityService.getPolicies();

		obs.subscribe((result) => {
			this.items = [];
			result.forEach((r) => {
				const menuItems = [
					{ title: "Edit", callback: () => { } },
					{ title: "Remove", callback: () => { } }
				];
				r.MenuItems = menuItems;

/*
				const menuItems = [];
				menuItems.push({ "title": $localize`View Information`, callback: () => { this.selected = rel; this.sidePanelService.setSidePanelState({ expanded: true }); } });
				menuItems.push({ "title": $localize`Open`, callback: () => this.open(rel.Uid) });
				// false poisitve fs.open eslint error
				// eslint-disable-next-line
				menuItems.push({ "title": $localize`Open In New Tab`, callback: () => this.open(rel.Uid, true) });

				menuItems.push({ "title": $localize`Edit`, callback: () => this.edit(rel), disabled: rel.IsEditDisabled, tooltip: this.getEditDisabledTooltip(rel) });
				menuItems.push({ "title": $localize`Delete`, callback: () => { this.editorSelectedUid = rel.Uid; this.showDelete = true; } });
				menuItems.push({ "title": $localize`Export`, callback: () => { this.downloadRel(rel); }, tooltip: $localize`Export all relationships in this type` });
				rel.MenuItems = menuItems;
*/
			});

			this.items = result;

			this.isLoading = false;
			if (this.items && this.items.length > 0) {
				this.selected = this.items[0];
					this.selectedChange.emit(this.selected);
			}
			this.checkGridState();
		});
	}

	private checkGridState() {
		if (sessionStorage.getItem(this.gridStorageKey)) {
			const gridData = JSON.parse(sessionStorage.getItem(this.gridStorageKey));

			if (gridData.filters && gridData.filters.global) {
				this.simpleFilterValue = gridData.filters.global.value;
			}

			this.cdRef.detectChanges();
		}
	}

	deleteItem($event) {
		this.showMessageForApiResult(this.messagesService, $event);
		this.showDelete = false;

		if ($event.Success === true) {
			this.selected = this.items.length > 0 ? this.items[0] : null;
			this.getData();
		}
	}

	onSave(result) {
		result = result[0];
		this.showMessageForApiResult(this.messagesService, result);

		if (result.Success === true) {
			this.getData();
			this.showEditor = false;
			this.sidePanelService.refreshSidePanel();
		}
	}

	closeEditor() {
		this.showEditor = false;
		if (this.selected == null) {
			this.selected = this.items.length > 0 ? this.items[0] : null;
		}
	}

	add() {
		this.showEditor = true;
		this.selected = null;
		this.editorSelectedUid = null;
	}

	edit(rel: ReadSecurityPolicy) {
		this.showEditor = true;
		this.selected = rel;
		this.editorSelectedUid = rel.uid;
	}

	get deletePromptText(): string {
		return $localize`Are you sure you want to delete the policy[${this.selected?.name}]?`;
	}

	positionContextMenu(
		$event: MouseEvent, container: HTMLElement, floatMenu: PopupMenu, gridTools: HTMLElement
	): void {
		if (!gridTools.contains(<Node>$event.target) && !this.isElementLink(<HTMLElement>$event.target)) {
			container.style.top = `${$event['layerY']}px`;
			container.style.left = `${$event['layerX']}px`;
			floatMenu.toggle($event);
			$event.preventDefault();
		}
	}

	private isElementLink(element: HTMLElement): boolean {
		while (element.parentElement) {
			if (element.tagName === 'A') { return true; }
			element = element.parentElement;
		}
		return false;
	}

	focusToPreselectedNode(preselectedUid: string) {
		try {
			this.selected = this.items.find((x) => x.uid === preselectedUid);
			this.selectedChange.emit(this.selected);

			//find index of topmost parent and naviate to its page
			const idx = this.items.indexOf(this.selected);
			const pageNumber = Math.floor(idx / this.dataTable.rows);

			if (pageNumber >= 0) {
				this.first = pageNumber * this.dataTable.rows;
				setTimeout(() => {
					//find preselected element and focus to it
					const htmlElement = document.querySelectorAll(`[data-uid='${preselectedUid}']`)[0] as HTMLElement;
					const treeTable = document.getElementsByClassName(`p-datatable-wrapper`)[0];
					treeTable.scrollTo({ top: htmlElement.offsetTop - 200 });
				}, 250);
			}
		}
		catch {
			// we want warning here instead of all ui breaking 
			// eslint-disable-next-line
			console.warn("failed to focus element");
		}
	}

	selectRow($event) {
		this.selected = $event;
		this.selectedChange.emit($event);
	}

	@ViewChildren('tableRow') tableRows: QueryList<ElementRef>;

	@HostListener('document:keydown.arrowup', ['$event'])
	@HostListener('document:keydown.arrowdown', ['$event'])
	onArrowKeysDownHandler($event: KeyboardEvent) {
		$event.preventDefault();
		const selectedRow = this.tableRows.toArray().find((elRef) => {
			return elRef.nativeElement.classList.contains('p-highlight');
		});

		if (typeof selectedRow === 'undefined') {
			this.tableRows.toArray()[0].nativeElement.click();
		}

		if (selectedRow && document.activeElement !== selectedRow.nativeElement) {
			selectedRow.nativeElement.dispatchEvent(
				new KeyboardEvent($event.type, { key: $event.key })
			);

			setTimeout(() => {
				//select newly focused element
				document.activeElement.dispatchEvent(new KeyboardEvent($event.type, { key: 'Enter' }));
			},0);
		}
	}
}
