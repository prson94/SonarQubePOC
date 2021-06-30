import { Component, OnInit, OnDestroy} from '@angular/core';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { OrganizationsService } from '../../../services/organizations.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Organization, OrganizationType } from '../../../models/organization.model';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';

@Component({
    selector: 'd3s-admin-organizations-component',
    providers: [OrganizationsService],
    template: `
<div class="col l4 m12">          
    <div class="tile tile-detail">
        <d3s-admin-organization-types [type]="selectedType" (typeChange)="selectOrganizationType($event)" ></d3s-admin-organization-types>
    </div>
    <div class="tile tile-detail"> 
        <d3s-admin-contracts></d3s-admin-contracts>
    </div>
</div>
<div class="col l8 m12">    
    <div class="row" *ngIf="selectedType">
        <div class="col s12">
            <div class="tile tile-detail">  
                <d3s-field-definition-tile [objectName]="selectedType?.Name" [objectType]="'OrganizationType'" [objectID]="selectedType.ID" [showIsListable]="false" [showIsPartOfKey]="false" [assetTypeUid]="selectedType?.uid"></d3s-field-definition-tile>
            </div>
        </div>
        <div class="col s12">
            <d3s-admin-organization-list-component [organizationType]="selectedType" [(organization)]="selected"></d3s-admin-organization-list-component>
        </div>
    </div>
    <div class="row" *ngIf="selected">        
        <div class="col s12">
            <div class="tile tile-detail">  
                <d3s-admin-organization-contracts [organization]="selected"></d3s-admin-organization-contracts>
            </div>
        </div>
        <div class="col s12">
            <div class="tile tile-detail">  
                <d3s-admin-organization-domains [organization]="selected"></d3s-admin-organization-domains>
            </div>
        </div>
        <div class="col s12">
            <div class="tile tile-detail">  
                <d3s-admin-organization-invitations [organization]="selected"></d3s-admin-organization-invitations>
            </div>
        </div>
        <div class="col s12">
            <div class="tile tile-detail">  
                <d3s-admin-organization-resources [organization]="selected"></d3s-admin-organization-resources>
            </div>
        </div>
    </div>
<div>
`
})

export class AdminOrganizationsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    selectedType: OrganizationType = null;
    selected: Organization;

    constructor(private router: Router, private stateService: StateService, secondaryNavService: SecondaryNavService, private organizationService: OrganizationsService, protected messagesService: MessagesObservableService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, secondaryNavService);        
        this.areaName = StringConstants.Section_Organizations;
        this.adminHeading = StringConstants.SubArea_Security;
        this.setCommonItems();
    }

    ngOnInit() {

    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    selectOrganizationType(e: any) {
        this.selectedType = e;
        if (this.selectedType != null) {
            this.setObjectInfo('OrganizationType', this.selectedType.ID);
        }
    }
}