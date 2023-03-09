import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Table } from 'primeng/table';
import { debounceTime, Observable, ReplaySubject, Subject, Subscription, takeUntil } from 'rxjs';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { FieldType } from '../../models/fieldtype-api.model';
import { Predicate, PredicateType } from '../../models/predicate.model';
import { DataCatalogService } from '../../services/dataCatalog.service';
import { FeatureFlags, FeatureFlagsService } from '../../services/featureflags.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { NumberOfRowsByCategoryService } from '../../services/number-of-rows-by-category.service';
import { PredicatesService } from '../../services/predicates.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { CompanySettingsService } from '../../services/settings.service';
import { AppConstants } from '../../static/constants';
import { AdvancedFilteringComponent } from '../assets-grid/advanced-filtering/advanced-filtering.component';
import { AdvancedFilterFieldType } from '../assets-grid/advanced-filtering/advanced-filtering.models';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { BaseComponent } from '../shared/base.component';

@Component({
	selector: 'd3s-data-catalog-grid',
	templateUrl: './data-catalog-grid.component.html',
	styleUrls: ['./data-catalog-grid.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class DataCatalogGridComponent extends AssetGridBaseComponent implements OnInit {
	subjectLoadGrid = new Subject<unknown>();
	predicates: Predicate[];
	columns: any[] = [];
	data: any[] = [];

	rowsPerPage: number = AppConstants.DEFAULT_ROWS_PER_PAGE;
	destroy = new Subject<void>();
	assetSearchSub: Subscription;
	totalRecords = 0;

	isContainsSearchDefault: boolean = false;
	simpleSearchText: string = '';
	advancedFilterText: string = '';

	public filterFields$: Observable<AdvancedFilterFieldType[]>;
	private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);
	@ViewChild("advancedFilter", { static: false }) advancedFilter: AdvancedFilteringComponent;
	@ViewChild("dt", { static: false }) dataTable: Table;

	constructor(
		private dataCatalogService: DataCatalogService,
		private predicateService: PredicatesService,
		settingsService: CompanySettingsService,
		public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
		private featureFlagService: FeatureFlagsService,
		private cdRef: ChangeDetectorRef,
		private titleService: Title,
		public headerBreadcrumbService: HeaderBreadcrumbService,
		public secondaryNavService: SecondaryNavService
	) {
		super(headerBreadcrumbService, settingsService, secondaryNavService);

		this.isContainsSearchDefault = this.featureFlagService.flags[FeatureFlags.ContainsSearchDefaultUiFlag];
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
		this.secondaryNavService.setCurrentArea(this.folderTitle ? this.folderTitle : this.area, 'fa-database', null);

	}

	ngOnDestroy() {
		if (this.assetSearchSub) {
			this.assetSearchSub.unsubscribe();
		}

		this.destroy.next();
		this.destroy.complete();
	}

	ngOnInit(): void {
		this.setRowsPerPage();
		this.numberOfRowsByCategoryService.defineNumberOfRows();

		this.predicateService.getPredicatesByType(PredicateType.CatalogBrowse)
			.subscribe((res) => {
				this.predicates = res;

				this.columns = [];
				this.columns.push({ columnName: $localize`Name`, apiProperty: "DisplayValue" });
				this.predicates.forEach((pred) => {
					this.columns.push({ columnName: pred.Name, apiProperty: pred.Name });
				});
				this.columns.push({ columnName: $localize`Asset Path`, apiProperty: "DisplayPath" });
				this.setFieldsObsservable();
				this.subjectLoadGrid.next(0);
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

		if (this.dataTable.sortField) {
			if (this.dataTable.sortOrder > 0) {
				params['_order'] = `asc(${this.dataTable.sortField})`;
			}
			else {
				params['_order'] = `desc(${this.dataTable.sortField})`;
			}
		}

		params["_pageNum"] = this.dataTable.first / this.dataTable.rows;
		params["_pageSize"] = this.dataTable.rows;


		this.assetSearchSub = this.dataCatalogService.getAssets(params).subscribe((res) => {
			this.data = res["items"];
			this.totalRecords = +res["total"];
			this.isLoading = false;
			this.cdRef.markForCheck();
		});
	}

	setRowsPerPage(): void {
		this.numberOfRowsByCategoryService.rowsPerPage.pipe(
			takeUntil(this.destroy)
		).subscribe((rowsPerPage) => {
			this.rowsPerPage = rowsPerPage['Main'];
		});
	}

	private setFieldsObsservable() {
		var fields: AdvancedFilterFieldType[] = [];
		fields.push({
			Name: "displayValue", FriendlyName: "Name", Type: new FieldType("Text"), Category: "", RemovePopulatedOperator: true
		});
		this.predicates.forEach((pred) => {
			const ft = new FieldType("Lookup");
			ft.Lookup.IsPrimaryFilter = true;
			fields.push({
				Name: pred.Name, FriendlyName: pred.Name, Type: ft, Category: "", RemovePopulatedOperator: true
			});
		});
		fields.push({
			Name: "displayPath", FriendlyName: "Asset Path", Type: new FieldType("Path"), Category: "", RemovePopulatedOperator: true
		});
		this.filterFieldsSubject.next(fields);
		this.filterFieldsSubject.complete();
	}

	public advancedFiltersChanged($event) {
		this.advancedFilterText = $event.filter as string;

		this.advancedFilterText = this.advancedFilterText.split(`'`).join(``).split('(').join(``).split(`)`).join(``);

		this.subjectLoadGrid.next(0);
	}
}
