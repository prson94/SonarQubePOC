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
                            <d3s-admin-relationships-list [(selected)]="selected"></d3s-admin-relationships-list>
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
        this.setCommonSecondaryNavTabs(true);    

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/IntersectType/${this.selected.Id}`
            });
        }

       
        let fields = new SecondaryNavItem()
        fields.hasDynamicUrl = true;
        fields.icons = ['fa-drivers-license-o'];
        fields.tag = 'fields'
        fields.title = 'Field Definitions'
        fields.url = '/sidebar/fields'
        fields.dynamicUrlCallback = (() => {
            return `/sidebar/fields/IntersectType/${this.selected.Id}`
        });

        this.secondaryNavService.showItem(fields);
      

        //this.secondaryNavService.showItem(new SecondaryNavItem('Relationship Roles', 'roles', ['fa-user']));
    }
    
    ngOnDestroy() {
        this.clearSidebar();
    }

    protected showHideBreadcrumbItem(activatedItem: SecondaryNavItem) {
        //if (activatedItem.tag == 'roles') this.isRolesVisible = !this.isRolesVisible; 
    }
}