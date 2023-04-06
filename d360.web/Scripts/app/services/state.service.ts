import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { SortOrder } from '../models/enums.model';
import {
	GridFilterExpression,
	GridOwnerFilter,
	GridRelationshipFilterExpression
} from '../models/grid-definition.model';

export class ArtifactTypeFilters {
	artifactTypeUid: string;
	simpleTextFilter: string;
	currentPageNumber: number = 0;
	sortField: string = "";
	sortOrder: SortOrder = SortOrder.None;
	filters: GridFilterExpression[] = [];
	relationships: GridRelationshipFilterExpression[] = [];
	owners: GridOwnerFilter;
	showSimpleFilter: boolean = true;
}

export class WorkflowItemFilters {
	itemId: number;
	stepId: number;
	currentPageNumber: number = 0;
	sortField: string = "";
	sortOrder: SortOrder = SortOrder.Descending;
	rowsPerPage: number = 10;
	columFilters: GridFilterExpression[] = [];
	workflowTypeFilters: GridFilterExpression;
}

export class GridSortData {
	private key: string = '';

	public sortField: string = "";
	public sortOrder: SortOrder = SortOrder.Descending;

	constructor(key: string) {
		this.key = key;
		let sortData = {};
		let sortDataString = window.localStorage.getItem("GridSortData");
		if (sortDataString !== null) {
			sortData = JSON.parse(sortDataString);
		}
		if (sortData[this.key] !== null && typeof sortData[this.key] !== "undefined") {
			this.sortField = sortData[this.key].sortField;
			this.sortOrder = sortData[this.key].sortOrder;
		}
	}

	save() {
		let sortData = {};
		let sortDataString = window.localStorage.getItem("GridSortData");
		if (sortDataString !== null) {
			sortData = JSON.parse(sortDataString);
		}
		if (sortData[this.key] === null || typeof sortData[this.key] === "undefined") {
			sortData[this.key] = { sortField: '', sortOrder: SortOrder.Descending };
		}

		sortData[this.key].sortField = this.sortField;
		sortData[this.key].sortOrder = this.sortOrder;
		window.localStorage.setItem("GridSortData", JSON.stringify(sortData));
	}
}

@Injectable()
export class StateService {
	constructor() {
		this.artifactTypeFilters = new ArtifactTypeFilters();
		this.workflowItemFilters = new WorkflowItemFilters();
	}
	public artifactTypeFilters: ArtifactTypeFilters;
	public workflowItemFilters: WorkflowItemFilters;
	private siteMenuRequiresReloadSource = new Subject<boolean>();
	private recalculateTagSizeSource = new Subject<void>();

	siteMenuRequiresReload$ = this.siteMenuRequiresReloadSource.asObservable();
	recalculateTagSize$ = this.recalculateTagSizeSource.asObservable();

	public resetArtifactTypeFilterIfRequired(artifactTypeUid: string) {
		if (this.artifactTypeFilters.artifactTypeUid !== artifactTypeUid) {
			this.artifactTypeFilters = new ArtifactTypeFilters();
			this.artifactTypeFilters.artifactTypeUid = artifactTypeUid;
		}
	}

	public resetWorkflowItemFilter() {
		this.workflowItemFilters = new WorkflowItemFilters();
	}

	reloadLeftNavMenu() {
		this.siteMenuRequiresReloadSource.next(true);
	}

	public recalculateTagSize() {
		this.recalculateTagSizeSource.next();
	}

}