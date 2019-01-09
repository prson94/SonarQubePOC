import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { SortOrder } from '../models/enums.model';
import { GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression, GridOwnerFilter } from '../models/grid-definition.model';
import { FusionAttributeFilter } from '../models/fusion-attribute.model';

export class ArtifactTypeFilters {
    artifactTypeId: number;
    simpleTextFilter: string;
    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];
    relationships: GridRelationshipFilterExpression[] = [];
    attributes: GridAttributeFilterExpression[] = [];
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

export class FusionFilters {
    id: number;
    type: string;
    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;
    rowsPerPage: number = 25;
    filters: FusionAttributeFilter[] = [];    
}

@Injectable()
export class StateService {
    constructor() {
        this.artifactTypeFilters = new ArtifactTypeFilters();       
        this.fusionFilters = new FusionFilters(); 
        this.workflowItemFilters = new WorkflowItemFilters();
    }
    public artifactTypeFilters: ArtifactTypeFilters;
    public fusionFilters: FusionFilters;
    public workflowItemFilters: WorkflowItemFilters;
    private siteMenuRequiresReloadSource = new Subject<boolean>();

    siteMenuRequiresReload$ = this.siteMenuRequiresReloadSource.asObservable();

    
    public resetArtifactTypeFilterIfRequired(artifactTypeId: number) {
        if (this.artifactTypeFilters.artifactTypeId != artifactTypeId) {            
            this.artifactTypeFilters = new ArtifactTypeFilters();
            this.artifactTypeFilters.artifactTypeId = artifactTypeId;
        }
    }

    public resetFusionAttributeFilterIfRequired(type: string, id: number) {
        if (this.fusionFilters.id != id || this.fusionFilters.type != type) {
            this.fusionFilters = new FusionFilters();
            this.fusionFilters.id = id;
            this.fusionFilters.type= type;
        }
    }

    public resetWorkflowItemFilter() {
        this.workflowItemFilters = new WorkflowItemFilters();
    }
    reloadLeftNavMenu() {
        this.siteMenuRequiresReloadSource.next(true);
    }

}