
import { Component, OnInit, OnDestroy} from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, PageHeader, RightSidebarService  } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component';
import { Relationship } from '../../models/relationship.model';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-relationships-component',
    template: `<d3s-audit *ngIf="isAuditVisible" [objectID]="selected?.ID" [objectName]="[selected?.SourceName] + ' / ' + [selected?.TargetName]" [objectType]="'IntersectType'"></d3s-audit>
                <div *ngIf="!isAuditVisible" class="row">
                    <div class="col l6 s12">                    
                        <div class="tile tile-detail">
                            <d3s-relationships-tile (onSelectedChanged)="selectedChanged($event)"></d3s-relationships-tile>
                        </div>
                    </div>                    
                    <div class="col l6 s12">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-predicates-tile></d3s-predicates-tile>
                                </div>
                            </div>
                        </div>
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
    
    selected: Relationship;
    
    constructor(rightSidebarService: RightSidebarService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService, rightSidebarService);
        this.areaDescription = "Create the possibility of establishing relationships between different objects within the system.";
        this.areaName = "Relationship Types";
        this.setCommonItems();
        this.setCommonRightSideBar(true);       
    }

    selectedChanged(selection) {        
        this.selected = selection;        
    }

    ngOnInit() {

    }

    ngOnDestroy() {
        this.clearSidebar();
    }
}