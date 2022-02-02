import { Component, OnInit, Input, OnDestroy } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { BaseComponent } from "../../shared/base.component";
import { SecondaryNavService } from "../../../services/right-sidebar.service";
import { HeaderBreadcrumbService } from "../../../services/header-breadcrumb.service";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: "d3s-comments",
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">
                        <d3s-social-board *ngIf="showBoard" [assetUid]="assetUid" [daysToLookBack]="daysToLookBack"></d3s-social-board>
                    </div>
                </div>
            </div>
        `
})

export class CommentsComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() assetUid: string="";
    @Input() localStorage: boolean = false;

    private sub: any;
    daysToLookBack: number = -1;
    hasCloseButton: boolean = false;
    showBoard: boolean = false;

    constructor(private route: ActivatedRoute,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {

        this.isLoading = true;
        this.showBoard = false;

        this.sub = this.route.params.subscribe(params => {
            this.assetUid = params["assetUid"];
            this.isLoading = false;
            this.showBoard = true;

            this.buildSecondaryNavigation(this.assetUid);
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}