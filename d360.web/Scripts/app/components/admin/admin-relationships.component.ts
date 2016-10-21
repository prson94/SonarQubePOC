import { Component, OnInit, OnDestroy} from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, PageHeader, RightSidebarService  } from '../../services/index';
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
                <div *ngIf="!isAuditVisible && !isPredicatesVisible" class="row">
                    <div class="col l6 s12">                    
                        <div class="tile tile-detail">
                            <d3s-admin-relationships-list (onSelectedChanged)="selectedChanged($event)"></d3s-admin-relationships-list>
                        </div>
                    </div>                    
                    <div class="col l6 s12">                        
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
    selected: Relationship;
    
    constructor(rightSidebarService: RightSidebarService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService, rightSidebarService);
        this.areaDescription = "Create the possibility of establishing relationships between different objects within the system.";
        this.areaName = "Relationship Types";
        this.setCommonItems();
        this.setCommonRightSideBar(true);    

        this.rightSidebarService.showItem(new RightSidebarItem('Predicates', 'predicates'));
    }

    selectedChanged(selection) {        
        this.selected = selection;        
    }

    ngOnInit() {

    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        if (activatedItem.tag == 'predicates') this.isPredicatesVisible = !this.isPredicatesVisible;        
    }
}