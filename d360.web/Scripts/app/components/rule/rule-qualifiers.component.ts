import { Component, OnInit, OnDestroy, Input, SimpleChange, OnChanges } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { QualifierService } from '../../services/qualifier.service';
import { QualifierType } from '../../models/qualifier.model';
import { FormMode } from '../../models/form.model';

@Component({
    selector: 'd3s-rule-qualifiers',
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">    
                            <d3s-rule-qualifier-grid [implementationId]="implementationId"></d3s-rule-qualifier-grid>
                        </div>
                    </div>
                </div>
          `,
    providers: [QualifierService],
})

export class RuleQualifiersComponent extends BaseComponent implements OnInit, OnDestroy {    
    private sub: any;
    private implementationId: number;
    
    constructor(                
        private route: ActivatedRoute,
        private router: Router,
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {            
            this.implementationId = +params['implementationId']; // (+) converts string 'id' to a number            
        });
        
    }
        
    ngOnDestroy() {
        if (this.sub) this.sub.unsubscribe();
    }    
}