import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-ownership',
    template: `                
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectID]="objectID" [objectType]="objectType" [title]="'Ownership of ' + objectName"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>
        `
})

export class OwnershipComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    
    constructor(
        private route: ActivatedRoute,
        private router: Router
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];            
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
    
}
