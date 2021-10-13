import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CompanySettingsService } from '../../../services/settings.service';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-diagram-wrapper',
    template: `           
                <d3s-model-diagram [id]="objectID"></d3s-model-diagram>                
        `
})

export class DiagramComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;

    constructor(
        protected settingsService: CompanySettingsService,
        private route: ActivatedRoute,
        private router: Router
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number       
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}
