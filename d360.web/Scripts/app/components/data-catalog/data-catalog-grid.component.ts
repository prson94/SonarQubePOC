import { Component, OnInit } from '@angular/core';
import { debounceTime, Subject } from 'rxjs';
import { Predicate, PredicateType } from '../../models/predicate.model';
import { DataCatalogService } from '../../services/dataCatalog.service';
import { PredicatesService } from '../../services/predicates.service';

@Component({
	selector: 'd3s-data-catalog-grid',
	templateUrl: './data-catalog-grid.component.html',
	styleUrls: ['./data-catalog-grid.component.less']
})
export class DataCatalogGridComponent implements OnInit {
	subjectLoadGrid = new Subject<unknown>();
	predicates: Predicate[];
	columns: any[] = [];
	data: any[] = [];

	constructor(
		private dataCatalogService: DataCatalogService,
		private predicateService: PredicatesService
	) {
		this.subjectLoadGrid.pipe(
			debounceTime(300))
			.subscribe(() => {
				this.loadData();
			});

	}

	ngOnInit(): void {
		this.predicateService.getPredicatesByType(PredicateType.CatalogBrowse)
			.subscribe((res) => {
				this.predicates = res;

				this.columns = [];
				this.columns.push({ columnName: $localize`Name`, apiProperty: "displayValue" });
				this.predicates.forEach((pred) => {
					this.columns.push({ columnName: pred.Name, apiProperty: pred.Name });
				});
				this.columns.push({ columnName: $localize`Asset Path`, apiProperty: "path" });
				this.subjectLoadGrid.next(0);
			});
	}

	loadData() {
		console.log("here");
		this.dataCatalogService.getAssets().subscribe((res) => {
			this.data = res["items"];
		});
	}
}
