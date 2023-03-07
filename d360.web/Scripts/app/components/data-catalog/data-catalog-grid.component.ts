import { Component, OnInit, ViewChild } from '@angular/core';
import { debounceTime, Observable, ReplaySubject, Subject, Subscription, takeUntil } from 'rxjs';
import { FieldType } from '../../models/fieldtype-api.model';
import { Predicate, PredicateType } from '../../models/predicate.model';
import { SearchFieldFilter } from '../../models/search-result.model';
import { DataCatalogService } from '../../services/dataCatalog.service';
import { FeatureFlags, FeatureFlagsService } from '../../services/featureflags.service';
import { NumberOfRowsByCategoryService } from '../../services/number-of-rows-by-category.service';
import { PredicatesService } from '../../services/predicates.service';
import { CompanySettingsService } from '../../services/settings.service';
import { AppConstants } from '../../static/constants';
import { AdvancedFilteringComponent } from '../assets-grid/advanced-filtering/advanced-filtering.component';
import { AdvancedFilterFieldType } from '../assets-grid/advanced-filtering/advanced-filtering.models';
import { BaseComponent } from '../shared/base.component';

@Component({
	selector: 'd3s-data-catalog-grid',
	templateUrl: './data-catalog-grid.component.html',
	styleUrls: ['./data-catalog-grid.component.less']
})
export class DataCatalogGridComponent extends BaseComponent implements OnInit {
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

	constructor(
		private dataCatalogService: DataCatalogService,
		private predicateService: PredicatesService,
		settingsService: CompanySettingsService,
		public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
		private featureFlagService: FeatureFlagsService
	) {
		super(settingsService);

		this.isContainsSearchDefault = this.featureFlagService.flags[FeatureFlags.ContainsSearchDefaultUiFlag];
		this.filterFields$ = this.filterFieldsSubject.asObservable();
		
		this.subjectLoadGrid.pipe(
			debounceTime(300))
			.subscribe(() => {
				this.loadData();
			});

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
				this.columns.push({ columnName: $localize`Name`, apiProperty: "displayValue" });
				this.predicates.forEach((pred) => {
					this.columns.push({ columnName: pred.Name, apiProperty: pred.Name });
				});
				this.columns.push({ columnName: $localize`Asset Path`, apiProperty: "path" });
				this.setFieldsObsservable();
				this.subjectLoadGrid.next(0);
			});
	}

	loadData() {
		console.log("here");
		this.isLoading = true;
		if (this.assetSearchSub) {
			this.assetSearchSub.unsubscribe();
		}
		this.assetSearchSub = this.dataCatalogService.getAssets().subscribe((res) => {
			this.data = res["items"];
			this.isLoading = false;
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
		this.filterFieldsSubject.next(fields);
		this.filterFieldsSubject.complete();
	}

	public advancedFiltersChanged($event) {
		this.advancedFilterText = $event.filter;
		this.subjectLoadGrid.next(0);
		console.log(this.advancedFilterText);
	}
}
