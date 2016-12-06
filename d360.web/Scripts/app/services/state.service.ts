import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/Subject';
import { SortOrder } from '../models/enums.model';
import { GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression, GridOwnerFilter } from '../models/grid-definition.model';

export class ArtifactTypeFilters {
    artifactTypeId: number;
    simpleTextFilter: string;
    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];
    relationships: GridRelationshipFilterExpression;
    attributes: GridAttributeFilterExpression;
    owners: GridOwnerFilter;
    showSimpleFilter: boolean = true;
}


@Injectable()
export class StateService {
    constructor() {
        this.artifactTypeFilters = new ArtifactTypeFilters();        
    }
    public artifactTypeFilters: ArtifactTypeFilters;
    private siteMenuRequiresReloadSource = new Subject<boolean>();

    siteMenuRequiresReload$ = this.siteMenuRequiresReloadSource.asObservable();

    
    public resetArtifactTypeFilterIfRequired(artifactTypeId: number) {
        if (this.artifactTypeFilters.artifactTypeId != artifactTypeId) {            
            this.artifactTypeFilters = new ArtifactTypeFilters();
            this.artifactTypeFilters.artifactTypeId = artifactTypeId;
        }
    }

    reloadLeftNavMenu() {
        this.siteMenuRequiresReloadSource.next(true);
    }

}