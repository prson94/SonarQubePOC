import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';

@Component({
    selector: 'd3s-permissions',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">
                        <d3s-responsibility-relations queryType="A" [id]="assetTypeId" [showAddButton]="false" showDeleteButton="true"></d3s-responsibility-relations>                        
                    </div>
                </div>
            </div>
        `,
    providers: [ObjectDetailService]
})

export class PermissionsComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    assetTypeId: number;    
    title: string;

    constructor(private objectDetailService: ObjectDetailService,
        private route: ActivatedRoute,
        private router: Router,
        private authenticationService: AuthenticationService,
        secondaryNavService: SecondaryNavService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        if (!this.authenticationService.isAdmin) {            
            this.router.navigateByUrl('/home');
        }
        this.sub = this.route.params.subscribe(params => {
            this.assetTypeId = +params['assetTypeId'];
   
        });
        this.checkSecondaryNavLocalStorage();
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
    
}