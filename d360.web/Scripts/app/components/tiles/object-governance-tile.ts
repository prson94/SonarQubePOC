///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ObjectHealthComponent } from '../shared/object-health.component';
import { ObjectBoardComponent } from '../shared/object-board.component';
import { ObjectIssuesComponent } from '../shared/object-issues.component';
import { ObjectChallengeComponent } from '../shared/object-challenge.component';

@Component({
    selector: 'd3s-object-governance-tile',    
    template: `     <div class="row">
                        <div class="col l3 s12">                                                        
                            <d3s-object-health [objectType]="objectType" [objectID]="objectID"></d3s-object-health>                            
                        </div>
                        <div class="col l3 s12">                                                        
                            <d3s-object-issues [objectType]="objectType" [objectID]="objectID"></d3s-object-issues>
                        </div>
                        <div class="col l3 s12">                                                        
                            <d3s-object-challenge [objectType]="objectType" [objectID]="objectID"></d3s-object-challenge>
                            
                        </div>
                        <div class="col l3 s12">
                            <d3s-object-board [objectType]="objectType" [objectID]="objectID"></d3s-object-board>                            
                        </div>
                    </div>
                `,
    directives: [ObjectHealthComponent, ObjectBoardComponent, ObjectIssuesComponent, ObjectChallengeComponent]
})

export class ObjectGovernanceTile extends BaseComponent implements OnInit {
    @Input() objectType: string;
    @Input() objectID: number;
 
    constructor() {
        super();
    }    

    ngOnInit() {

    }
}
