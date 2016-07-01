///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';

@Component({
    selector: 'd3s-admin-relationships-component',
    directives: [DataTable, Column, TileActionsComponent],
    template: `<div class="row">
                    <div class="col l6 s12">                    
                        <div class="tile tile-detail">
                            <header>Relationship Types
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Relationship'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>                                                                               
                        </div>
                    </div>                    
                    <div class="col l6 s12">
                        <div class="tile tile-detail">
                            <header>Predicates
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Predicate'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>                                                                          
                        </div>
                    <div>
                </div>  
                `
})

export class AdminRelationshipsComponent extends AdminBaseComponent {


    constructor(protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Create the possibility of establishing relationships between different objects within the system.";
        this.areaName = "Relationship Types";
        this.setCommonItems();
    }

    ngOnInit() {

        this.getRelationships();
    }

    getRelationships() {

    }
}