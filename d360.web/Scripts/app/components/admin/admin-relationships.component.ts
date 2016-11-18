import { Component, OnInit, OnDestroy} from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, RightSidebarService  } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component';
import { Relationship } from '../../models/relationship.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';

@Component({
    selector: 'd3s-admin-relationships-component',
    template: `<d3s-audit *ngIf="isAuditVisible" [objectID]="selected?.ID" [objectName]="[selected?.SourceName] + ' / ' + [selected?.TargetName]" [objectType]="'IntersectType'"></d3s-audit>
                <div *ngIf="isPredicatesVisible" class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-predicates-list></d3s-predicates-list>
                        </div>
                    </div>
                </div>
                <div *ngIf="isRolesVisible" class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-admin-relationship-roles></d3s-admin-relationship-roles>
                        </div>
                    </div>
                </div>
                <div *ngIf="!isAuditVisible && !isPredicatesVisible && !isRolesVisible" class="row">
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

export class AdminRelationshipsComponent extends AdminBaseComponent implements OnDestroy, OnInit {
    private isPredicatesVisible: boolean = false;
    private isRolesVisible: boolean = false;
    private selected: Relationship;
    
    constructor(rightSidebarService: RightSidebarService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService,  titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Relationship Types";
        this.setCommonItems();
        this.setCommonRightSideBar(true);    

        this.rightSidebarService.showItem(new RightSidebarItem('Predicates', 'predicates', ['fa-map-signs']));
        this.rightSidebarService.showItem(new RightSidebarItem('Relationship Roles', 'roles', ['fa-user']));
    }

    
    ngOnInit() {

    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        if (activatedItem.tag == 'predicates') this.isPredicatesVisible = !this.isPredicatesVisible;        
        else if (activatedItem.tag == 'roles') this.isRolesVisible = !this.isRolesVisible; 
    }
}