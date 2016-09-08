///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { SortOrder } from '../models/enums.model';
import { GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../models/grid-definition.model';

export class ArtifactTypeFilters {
    artifactTypeId: number;
    simpleTextFilter: string;
    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];
    relationships: GridRelationshipFilterExpression;
    attributes: GridAttributeFilterExpression;
}


@Injectable()
export class StateService {
    constructor() {
        this.artifactTypeFilters = new ArtifactTypeFilters();        
    }
    public artifactTypeFilters: ArtifactTypeFilters;
    
    public resetArtifactTypeFilterIfRequired(artifactTypeId: number) {
        if (this.artifactTypeFilters.artifactTypeId != artifactTypeId) {
            this.artifactTypeFilters = new ArtifactTypeFilters();
            this.artifactTypeFilters.artifactTypeId = artifactTypeId;
        }
    }
}