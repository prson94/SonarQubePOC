///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';

@Component({
    selector: 'd3s-ownership-tab',
    template: `                
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectType]="objectType" [objectID]="objectID" [title]="'Ownership of ' + objectName"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>
        `,
    directives: [PeopleResponsibilitiesTile]
})

export class OwnershipTabComponent extends BaseComponent {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;
    

    constructor() {
        super();
    }    
}