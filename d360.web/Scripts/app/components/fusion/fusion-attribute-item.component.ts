import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionService } from '../../services/fusion.service';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { FusionConfigurationDetails, FusionAttributeType  } from '../../models/fusion.model';
import { FusionStructureTreeComponent} from './fusion-structure-tree.component';
import { FusionAttributeFilter } from '../../models/fusion-attribute.model';
import { RightSidebarItem } from '../../models/rightsidebar.model';
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