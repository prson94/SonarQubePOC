import { Component, OnDestroy} from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { RelationshipType } from '../../../models/relationship.model';
import { Title } from '@angular/platform-browser';
import { SecondaryNavItem } from '../../../models/secondaryNav.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-relationships-component',
    template: `<div class="row">
                    <div class="col l12 m12 s12">                    
                        <div class="tile tile-detail">
                            <d3s-admin-relationships-list (selectedChange)="selectedItemChange($event)" [(selected)]="selected"></d3s-admin-relationships-list>
                        </div>
                    </div>                    
                </div>  
                `
})

export class AdminRelationshipsComponent extends AdminBaseComponent implements OnDestroy {   
    private selected: RelationshipType;
    
    constructor(secondaryNavService: SecondaryNavService, protected messagesService: MessagesObservableService, headerBreadcrumbService: HeaderBreadcrumbService,  titleService: Title) {
        super(headerBreadcrumbService, titleService, secondaryNavService);        
        this.areaName = "Relationships";
        this.tabTitle = "Relationship Types";
        this.setCommonItems();
        this.setCommonSecondaryNavTabs(true, null, null, null, null, null, null, null, true);
    }

    selectedItemChange(event) {
        this.selected = event;
        if (this.auditSidebar && this.selected) {
            this.auditSidebar.url = `/sidebar/audit/IntersectType/${this.selected.Id}`;
        }
        if (this.fieldNav && this.selected) {
            this.fieldNav.url = `/sidebar/fields/IntersectType/${this.selected.Id}`;
        }       
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    protected showHideBreadcrumbItem(activatedItem: SecondaryNavItem) {
        //if (activatedItem.tag == 'roles') this.isRolesVisible = !this.isRolesVisible; 
    }
}