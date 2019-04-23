import {Component, Input, OnInit, OnDestroy} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {BaseComponent} from '../../shared/base.component';
import {ObjectDetailService} from '../../../services/object-detail.service';
import {FusionService} from '../../../services/fusion.service';

@Component({
    selector: 'd3s-ownership',
    template: `
        <div class="row">
            <div class="col s12">
                <div class="tile tile-detail">
                    <d3s-people-responsibilities-tile [assetID]="assetID"
                                                      [title]="'Ownership of ' + [objectName]"></d3s-people-responsibilities-tile>
                </div>
            </div>
        </div>
    `,
    providers: [
        ObjectDetailService,
        FusionService
    ]
})

export class OwnershipComponent extends BaseComponent implements OnInit, OnDestroy {

    private sub: any;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private objectDetailService: ObjectDetailService,
        private fusionservice: FusionService
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(
            params => {
                this.assetID = +params['assetID'];

                this.objectDetailService.getAsset(this.assetID).subscribe(
                    res => {
                        if (res.Type == "FusionType") {
                            this.fusionservice.getFusionConfigurationsByType(res.TypeID).then(fus => {
                                this.objectName = fus[0].Name;
                            })
                        } else {
                            this.objectName = res.DisplayValue;
                        }
                    }
                );
            }
        );
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}
