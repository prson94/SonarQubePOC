import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionService } from '../../services/fusion.service';
import { BaseComponent } from '../shared/base.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-fusion-attribute-item',
    template: ` `,
    providers: [FusionService],
})

export class FusionAttributeItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;

    constructor(
        private fusionService: FusionService,
        private route: ActivatedRoute,
        private router: Router) {
        super();        
    }

    ngOnInit() {
        
        this.sub = this.route.params.subscribe(params => {                        
            var fusionAttributeTypeId = +params['fusionAttributeTypeId'];
            var fusionAttributeId = +params['fusionAttributeId'];            
            this.fusionService.getFusionConfigurationFromAttributeId(fusionAttributeId)
                .then(res => {
                    this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${res.ID};fusionAttributeTypeId=${fusionAttributeTypeId};fusionAttributeId=${fusionAttributeId}`);                    
                });

        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }    
};