import { Component, OnDestroy} from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component';
import { RelationshipType } from '../../../models/relationship.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../../models/rightsidebar.model';

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
    
    constructor(rightSidebarService: RightSidebarService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService,  titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Relationship Types";
        this.setCommonItems();
        this.setCommonRightSideBar(true);    

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/IntersectType/${this.selected.Id}`
            });
        }

       
        let fields = new RightSidebarItem()
        fields.hasDynamicUrl = true;
        fields.icons = ['fa-drivers-license-o'];
        fields.tag = 'fields'
        fields.title = 'Field Definitions'
        fields.url = '/sidebar/fields'
        fields.dynamicUrlCallback = (() => {
            return `/sidebar/fields/IntersectType/${this.selected.Id}`
        });

        this.rightSidebarService.showItem(fields);
      

        //this.rightSidebarService.showItem(new RightSidebarItem('Relationship Roles', 'roles', ['fa-user']));
    }
    
    ngOnDestroy() {
        this.clearSidebar();
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        //if (activatedItem.tag == 'roles') this.isRolesVisible = !this.isRolesVisible; 
    }
}