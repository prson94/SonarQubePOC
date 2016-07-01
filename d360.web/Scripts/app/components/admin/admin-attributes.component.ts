///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';

@Component({
    selector: 'd3s-admin-attributes-component',
    directives: [DataTable, Column, TileActionsComponent],    
    template: `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor">Attribute Types
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Attribute'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>  
                            <div *ngIf="isLoading">
                                <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                            </div>                                                      
                        </div>
                    </div>                    
                    <div class="col l8 s12">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">           
                                    
                                </div>
                            </div>
                        </div>
                    <div>
                </div>  
                `
})

export class AdminAttributesComponent extends AdminBaseComponent {
    

    constructor(protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Here you will find all metadata that can be assigned to various objects and relationships.";
        this.areaName = "Attribute Groups";
        this.setCommonItems();
    }

    ngOnInit() {

        this.getAttributes();
    }

    getAttributes() {
        
    }
}