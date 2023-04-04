import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';
import { Table } from 'primeng/table';
import { debounceTime, Observable, ReplaySubject, Subject, Subscription, takeUntil } from 'rxjs';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { FieldType } from '../../models/fieldtype-api.model';
import { Predicate, PredicateType } from '../../models/predicate.model';
import { DataCatalogService } from '../../services/dataCatalog.service';
import { FeatureFlags } from '../../services/feature-flags.enum';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { NumberOfRowsByCategoryService } from '../../services/number-of-rows-by-category.service';
import { PredicatesService } from '../../services/predicates.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { CompanySettingsService } from '../../services/settings.service';
import { SidePanelService } from '../../services/side-panel.service';
import { AppConstants } from '../../static/constants';
import { AdvancedFilteringComponent } from '../assets-grid/advanced-filtering/advanced-filtering.component';
import { AdvancedFilterFieldType } from '../assets-grid/advanced-filtering/advanced-filtering.models';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { PopupMenu } from '../shared/controls/popup-menu/popup-menu.component';

/*global $localize*/

@Component({
	selector: 'd3s-data-catalog-grid',
	templateUrl: './data-catalog-grid.component.html',
	styleUrls: ['./data-catalog-grid.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class DataCatalogGridComponent extends AssetGridBaseComponent implements OnInit {
	subjectLoadGrid = new Subject<string>();
	predicates: Predicate[];
	columns: Record<string, unknown>[] = [];
	data: Record<string, unknown>[] = [];

	numberOfRowsStorageKey = 'DataCatalogRowsPerPage';

	rowsPerPage: number = AppConstants.DEFAULT_ROWS_PER_PAGE;
	destroy = new Subject<void>();
	assetSearchSub: Subscription;
	totalRecords = 0;

	isContainsSearchDefault: boolean = false;
	simpleSearchText: string = '';
	advancedFilterText: string = '';
	selected: Record<string, unknown> = null;

	public filterFields$: Observable<AdvancedFilterFieldType[]>;
	private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);
	@ViewChild("advancedFilter", { static: false }) advancedFilter: AdvancedFilteringComponent;
	@ViewChild("dt", { static: false }) dataTable: Table;

	constructor(
		private dataCatalogService: DataCatalogService,
		private predicateService: PredicatesService,
		public sidePanelService: SidePanelService,
		settingsService: CompanySettingsService,
		private featureFlagService: LaunchDarklyService,
		private cdRef: ChangeDetectorRef,
		private titleService: Title,
		public headerBreadcrumbService: HeaderBreadcrumbService,
		public secondaryNavService: SecondaryNavService,
		private router: Router
	) {
		super(headerBreadcrumbService, settingsService, secondaryNavService);

		this.isContainsSearchDefault = this.featureFlagService.variation<boolean>(FeatureFlags.ContainsSearchDefaultUiFlag);
		this.filterFields$ = this.filterFieldsSubject.asObservable();

		this.subjectLoadGrid.pipe(
			debounceTime(300))
			.subscribe(() => {
				this.loadData();
			});

		this.folderTitle = $localize`Data Catalog`;
		this.setBrowserTitle(this.titleService, this.folderTitle);
		this.area = this.folderTitle;

		this.headerBreadcrumbService.clearBreadcrumbs();
		this.headerBreadcrumbService.clearCurrentObjectInfo();
		this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.folderTitle ? this.folderTitle : this.area));
		this.secondaryNavService.clearCurrentObject();
		this.secondaryNavService.clearItems();
		this.secondaryNavService.setCurrentArea(this.folderTitle ? this.folderTitle : this.area, 'gov-data-catalog-icon-white', null);

	}

	ngOnDestroy() {
		if (this.assetSearchSub) {
			this.assetSearchSub.unsubscribe();
		}

		this.destroy.next();
		this.destroy.complete();
	}

	ngOnInit(): void {
		this.loadRowsPerPage();

		this.predicateService.getPredicatesByType(PredicateType.CatalogBrowse)
			.subscribe((res) => {
				this.predicates = res;

				this.columns = [];
				this.columns.push({ columnName: $localize`Name`, apiProperty: "DisplayValue", minWidth: "300px" });
				this.predicates.forEach((pred) => {
					this.columns.push({ columnName: pred.Name, apiProperty: pred.Name, minWidth: "150px" });
				});
				this.columns.push({ columnName: $localize`Asset Path`, apiProperty: "DisplayPath" });
				this.setFieldsObsservable();
			});
	}

	loadData() {
		this.isLoading = true;
		if (this.assetSearchSub) {
			this.assetSearchSub.unsubscribe();
		}

		const params = {};

		if (this.advancedFilterText) {
			params['_filter'] = this.advancedFilterText;
		}

		if (this.simpleSearchText) {
			params['_simpleFilter'] = this.simpleSearchText;

			if (this.isContainsSearchDefault) {
				params['_simpleFilter'] = "*" + this.simpleSearchText;
			}
		}

		if (this.dataTable.sortField) {
			if (this.dataTable.sortOrder > 0) {
				params['_order'] = `asc(${this.dataTable.sortField})`;
			}
			else {
				params['_order'] = `desc(${this.dataTable.sortField})`;
			}
		}

		params["_pageNum"] = (this.dataTable.first / this.dataTable.rows) + 1;
		params["_pageSize"] = this.dataTable.rows;


		this.assetSearchSub = this.dataCatalogService.getAssets(params).subscribe((res) => {
			this.data = res["items"];
			this.totalRecords = +res["total"];

			this.data.forEach((item) => {
				const menuItems = [];
				// false poisitve fs.open eslint error
				// eslint-disable-next-line
				menuItems.push({ "title": $localize`Open`, callback: () => this.open(item.Uid) });
				// false poisitve fs.open eslint error
				// eslint-disable-next-line
				menuItems.push({ "title": $localize`Open In New Tab`, callback: () => this.open(item.Uid, true) });
				item.MenuItems = menuItems;
			});

			this.isLoading = false;
			this.cdRef.markForCheck();
		});
	}

	loadRowsPerPage(): void {
		const rowsPerPageStorage = localStorage.getItem(this.numberOfRowsStorageKey);
		this.rowsPerPage = rowsPerPageStorage != null ? +rowsPerPageStorage : 25;
	}

	setRowsPerPage($event) {
		if ($event && $event.rows) {
			localStorage.setItem(this.numberOfRowsStorageKey, $event.rows);
		}
	}

	private setFieldsObsservable() {
		const fields: AdvancedFilterFieldType[] = [];
		fields.push({
			Name: "displayValue", FriendlyName: "Name", Type: new FieldType("Text"), Category: "", RemovePopulatedOperator: true
		});
		this.predicates.forEach((pred) => {
			const ft = new FieldType("Lookup");
			ft.Lookup.IsPrimaryFilter = true;
			ft.Lookup.List.AllowMultipleValues = true;
			fields.push({
				Name: pred.Name, FriendlyName: pred.Name, Type: ft, Category: ""
			});
		});
		const pathType = new FieldType("Path");
		pathType.Path.Definition = null;
		fields.push({
			Name: "displayPath", FriendlyName: "Asset Path", Type: pathType, Category: "", RemovePopulatedOperator: true
		});
		this.filterFieldsSubject.next(fields);
		this.filterFieldsSubject.complete();
	}

	public advancedFiltersChanged($event) {
		this.advancedFilterText = $event.filter as string;

		this.advancedFilterText = this.advancedFilterText.split(`'`).join(``);
		this.subjectLoadGrid.next("advancedFiltersChanged");
	}
	selectRow($event) {
		this.sidePanelService.setSidePanelState({ assetUid: $event.Uid });
	}

	open(uid, newTab: boolean = false) {
		const url = `/asset/${uid}`;
		if (newTab) {
			// false poisitve fs.open eslint error
			// eslint-disable-next-line
			window.open(url, "_blank");
		}
		else {
			this.router.navigateByUrl(this.federateUrl(url));
		}
	}

	positionContextMenu(
		$event: MouseEvent, container: HTMLElement, floatMenu: PopupMenu, assetGridTools: HTMLElement
	): void {
		if (!assetGridTools.contains(<Node>$event.target) && !this.isElementLink(<HTMLElement>$event.target)) {
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
}
