import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { AuthenticationService } from '../../../services/authentication.service';

@Component({
    selector: 'd3s-permissions',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">
                        <d3s-responsibility-relations queryType="A" [id]="assetTypeId" showAddButton="true" showDeleteButton="true"></d3s-responsibility-relations>                        
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
        private authenticationService: AuthenticationService
    ) {
        super();
    }

    ngOnInit() {
        if (!this.authenticationService.isAdmin) {            
            this.router.navigateByUrl('/home');
        }
        this.sub = this.route.params.subscribe(params => {
            this.assetTypeId = +params['assetTypeId'];
   
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
    
}