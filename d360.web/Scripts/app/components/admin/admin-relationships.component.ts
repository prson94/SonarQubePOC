///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, PageHeader  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { PredicatesTile } from '../tiles/predicates.tile';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { Relationship } from '../../models/relationship.model';
import { RelationshipsTile } from '../tiles/relationships.tile'

@Component({
    selector: 'd3s-admin-relationships-component',
    directives: [PredicatesTile, FieldDefinitionTile, RelationshipsTile],
    template: `<div class="row">
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

export class AdminRelationshipsComponent extends AdminBaseComponent {
    
    selected: Relationship;
    
    constructor(protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Create the possibility of establishing relationships between different objects within the system.";
        this.areaName = "Relationship Types";
        this.setCommonItems();        
    }

    selectedChanged(selection) {        
        this.selected = selection;        
    }
    
}