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
    template: `<div *ngIf="isRolesVisible" class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-admin-relationship-roles></d3s-admin-relationship-roles>
                        </div>
                    </div>
                </div>
                <div *ngIf="!isRolesVisible" class="row">
                    <div class="col l6 s12">                    
                        <div class="tile tile-detail">
                            <d3s-admin-relationships-list [(selected)]="selected"></d3s-admin-relationships-list>
                        </div>
                    </div>                    
                    <div class="col l6 s12" *ngIf="selected">                        
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile [objectType]="'IntersectType'" [objectID]="selected?.ID" ></d3s-field-definition-tile>
                                </div>
                            </div>
                        </div>
                    <div>                    
                </div>  
                `
})

export class AdminRelationshipsComponent extends AdminBaseComponent implements OnDestroy {   
    private isRolesVisible: boolean = false;
    private selected: RelationshipType;
    
    constructor(rightSidebarService: RightSidebarService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService,  titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Relationship Types";
        this.setCommonItems();
        this.setCommonRightSideBar(true);    
                
        //this.rightSidebarService.showItem(new RightSidebarItem('Relationship Roles', 'roles', ['fa-user']));
    }
    
    ngOnDestroy() {
        this.clearSidebar();
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        if (activatedItem.tag == 'roles') this.isRolesVisible = !this.isRolesVisible; 
    }
}