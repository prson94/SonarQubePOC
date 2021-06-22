import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { SortOrder } from '../models/enums.model';
import { GridFilterExpression, GridRelationshipFilterExpression, GridOwnerFilter } from '../models/grid-definition.model';

export class ArtifactTypeFilters {
    artifactTypeId: number;
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
    rowsPerPage: number =10;
    columFilters: GridFilterExpression[] = [];
    workflowTypeFilters: GridFilterExpression;
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

    siteMenuRequiresReload$ = this.siteMenuRequiresReloadSource.asObservable();

    public resetArtifactTypeFilterIfRequired(artifactTypeId: number) {
        if (this.artifactTypeFilters.artifactTypeId != artifactTypeId) {            
            this.artifactTypeFilters = new ArtifactTypeFilters();
            this.artifactTypeFilters.artifactTypeId = artifactTypeId;
        }
    }

    public resetWorkflowItemFilter() {
        this.workflowItemFilters = new WorkflowItemFilters();
    }
    reloadLeftNavMenu() {
        this.siteMenuRequiresReloadSource.next(true);
    }

}