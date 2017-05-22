import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { FusionConfigurationDetails } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-history',
    template: `
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12">
                        <d3s-fusion-execution-history [fusion]="fusion"></d3s-fusion-execution-history>
                        <d3s-fusion-agent-history [fusion]="fusion"></d3s-fusion-agent-history>
                    </div>
                </div>    
        `,
    providers: [FusionService],
})

export class FusionHistoryComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private fusion: FusionConfigurationDetails;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        protected titleService: Title,
        private fusionService: FusionService
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            let fusionId = +params['fusionId']; // (+) converts string 'id' to a number
            this.isLoading = true;
            this.fusionService.getFusionConfiguration(fusionId)
                .then(result => {                    
                    this.fusion = result;
                    this.isLoading = false;
                    this.setBrowserTitle(this.titleService, `History of Fusion - ${this.fusion.Name}`);                    
                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}
