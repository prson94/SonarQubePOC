///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { DomainType, IDomainService } from '../../models/domain.model';
import { DomainService } from '../../services/domain.service';
import { DataTable, Column } from 'primeng/primeng';
import { AdminBaseComponent} from './admin-base.component';

@Component({
    selector: 'admin-domain',
    providers: [DomainService],
    directives: [ObjectDetailTile, FieldDefinitionTile, PeopleResponsibilitiesTile, DataTable, Column],
    templateUrl: 'scripts/app/components/admin/admin-domain.component.html',
})

export class AdminDomainComponent extends AdminBaseComponent {
    domainTypes = new Array<DomainType>(); 
    objectType = 'DomainType';
    selectedRow: DomainType;
    

    constructor(private domainService: DomainService, pageHeader: PageHeader, headerBreadcrumbService : HeaderBreadcrumbService) {        
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "All type of reference data lists for the organization are defined here. To add a new type of list, go under Actions and select Add type.";
        this.areaName = "Reference Types";
        this.setCommonItems();

        this.load();
    }

    load() {

        this.isLoading = true;
        this.domainService.getDomains()
            .then(data => {
                this.domainTypes = data;
                this.selectedRow = this.domainTypes[0];
                this.isLoading = false;
            });       
    }
}