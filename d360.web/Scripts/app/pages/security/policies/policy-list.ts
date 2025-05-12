import { ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, Output, QueryList, SimpleChange, ViewChild, ViewChildren, ViewEncapsulation } from '@angular/core';
import { Table, TableModule } from "primeng/table";
import { BaseComponent } from '../../../components/shared/base.component';
import { PopupMenu, PopupMenuModule } from '../../../components/shared/controls/popup-menu/popup-menu.component';
import { ApiResult } from '../../../models/apiresult.model';
import { ReadSecurityPolicy, PolicyEditOptionsModel } from '../../../models/security.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { SecurityService } from '../../../services/security.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { SidePanelService } from '../../../services/side-panel.service';
import { AppConstants } from '../../../static/constants';
import { SearchFieldModule } from '../../../components/shared/controls/search-field/search-field.component';
import { DataCyModule } from '../../../directives/ig-data-cy.directive';
import { ButtonModule } from '../../../directives/ig-button-directive';
import { SortIconComponent } from '../../../_shared/components/sort-icon';
import { SharedGridPagingInfoModule } from '../../../components/shared/grid-paging-info.component';
import { PolicyDelete } from './policy-delete';
import { PolicyEditor } from './policy-editor';
import { DirectivesModule } from '../../../directives/directives.module';
import { FormsModule } from '@angular/forms';

@Component({
	selector: 'policy-list',
	templateUrl: 'policy-list.html',
	styleUrls: ['policy-list.less'],
	encapsulation: ViewEncapsulation.None,
	standalone: true,
	imports: [
		ButtonModule,
		DirectivesModule,
		DataCyModule,
		FormsModule,
		PolicyDelete,
		PolicyEditor,
		PopupMenuModule,
		SearchFieldModule,
		SharedGridPagingInfoModule,
		SortIconComponent,
		TableModule
	]
})
export class PolicyList extends BaseComponent implements OnChanges {
	items: ReadSecurityPolicy[] = [];

	rowsPerPage: number = AppConstants.DEFAULT_ROWS_PER_PAGE;
	defaultPagingOptions = AppConstants.DEFAULT_PAGING_OPTIONS;

	@Input() filterToName: string = "";

	selected: ReadSecurityPolicy;
	@Output() selectedChange = new EventEmitter();

	options: PolicyEditOptionsModel;

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
		this.loadEditOptions();
	}

	loadEditOptions() {
		// Pre-populate common lists. 
		this.securityService.getPolicyEditOptions().subscribe((o) => {
			//o.assetTypes = o.assetTypes.sort((a, b) => (a.label < b.label ? -1 : 1));
			//o.roles = o.roles.sort((a, b) => (a.label < b.label ? -1 : 1));
			this.options = o;
		});
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if ( changes['filterToName'] && changes['filterToName'].currentValue !== changes['filterToName'].previousValue ) {
			this.getData();
		}
	}

	getData() {
		this.isLoading = true;
		const obs = this.securityService.getPolicies();

		obs.subscribe((result) => {
			this.items = [];
			result.forEach((r) => {
				const menuItems = [
					{ title: "Edit", callback: () => { this.edit(r); } },
					{ title: "Remove", callback: () => { this.showDelete = true; } }
				];
				r.MenuItems = menuItems;
			});

			this.items = result;

			this.isLoading = false;
			if (this.items && this.items.length > 0) {
				this.selected = this.items[0];
					this.selectedChange.emit(this.selected);
			}
			this.checkGridState();
		}, (error) => {
			this.showHttpErrorMessage(this.messagesService, error);
			this.isLoading = false;
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
		const response: ApiResult = new ApiResult();
		response.Success = true;
		response.Message = "Removed policy successfully.";
		this.showMessageForApiResult(this.messagesService, response);
		this.showDelete = false;

		const deletedIndex = this.items.findIndex(i => i.uid === this.selected.uid)
		if (deletedIndex >= 0) {
			const tempItems = this.items;
			tempItems.splice(deletedIndex, 1);
			this.items = tempItems;
		}
		this.selected = this.items.length > 0 ? this.items[0] : null;
		this.checkGridState();
		//this.getData();
	}

	onSave(result) {
		if (result) {
			const response: ApiResult = new ApiResult();
			response.Success = true;

			if (result.uid) {
				const updateIx = this.items.findIndex(i => { return i.uid === result.uid; });
				if (updateIx >= 0) {
					response.Message = "Updated policy successfully.";
				}
				else {
					response.Message = "Created policy successfully.";
				}
			}
			this.showMessageForApiResult(this.messagesService, response);

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
