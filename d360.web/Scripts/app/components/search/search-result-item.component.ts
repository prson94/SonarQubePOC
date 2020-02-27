import { Component, Input, ChangeDetectionStrategy, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchFullResult, SearchDetail } from '../../models/search-result.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { ShoppingCartService } from '../../services/shopping-cart.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { ObjectStatistics } from '../../models/object-statistics.model';
import { TagService } from '../../services/tag.service';
import { Tag, TagItem } from '../../models/tag.model';
import { ObjectStatisticsService } from '../../services/object-statistics.service';

declare var CompanySettings;

@Component({
    selector: 'd3s-search-result-item',
    template: `       
                <div class="card-res">
                    <span class="title">
                        <span *ngIf="result?.Icon" class="d3s-icon large-icon title-icon"><i class="fa {{result?.Icon}}"></i></span>
                        <span *ngIf="result?.ImageUrl" class="d3s-icon large-icon title-icon"><img [src]="result.ImageUrl" /></span> 
                        <span (click)="navigateLink()" class="name"><span class="inner" [innerHtml]="result?.Name"></span></span>
                        <d3s-status-badge
                            *ngIf="showStatus"
                            [status]="status"
                        >
                        </d3s-status-badge>
                        <span #scorecontainer *ngIf="searchDetails && searchDetails.Scores.length > 0" class="d3s-score-container">
                            <d3s-score-badge *ngFor="let score of searchDetails.Scores" [score]="score">
                            </d3s-score-badge>
                        </span>
                        <button *ngIf="showShoppingCart && result.Group != 'Synonym' && result.Group != 'Attribute'" class="button icon" (click)="add()">
                            <i class="fa fa-cart-plus"></i>
                        </button>
                        <span *ngIf="result.Uid" class="d3s-icon smallmed-icon light">
                            <d3s-preview-tooltip [uid]="result.Uid" icon="info-circle"></d3s-preview-tooltip>
                        </span>
                    </span>
                    <span class="category">
                        {{result?.Group}}<span *ngIf="result?.Type"><i class="fa fa-angle-right"></i><span class="category" [innerHtml]="result?.Type"></span></span>
                    </span>
                    <span class="asset-path" *ngIf="searchDetails && searchDetails?.AssetDetail?.Path">
                        {{formattedPath}}
                    </span>
                    <span class="description" *ngIf="result?.Description" [innerHtml]="result.Description"></span>
                    <div *ngIf="result?.Tags" class="tags tagsnomanagewidth">
                        <d3s-tag-view [ignoreResizing]="true" [data]="parseTagResult(result?.Tags)"></d3s-tag-view>
                    </div>
                    <div *ngIf="result?.Explaination"><explain-widget [json]="result?.Explaination"></explain-widget></div>
                </div>        
                `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ShoppingCartService, ObjectStatisticsService]
})

export class SearchResultItemComponent extends BaseComponent implements OnInit {
    @Input() result: SearchFullResult;
    private lastCalculatedDate: number;
    private showStatus: boolean = false;
    private showPath: boolean = false;
    private status: string;
    private path: string;
    showShoppingCart = false;
    private obj: string;
    private objID: number;
    private searchDetails: SearchDetail;
    private formattedPath: string;

    parseTagResult(tags: any[]) {
        return tags.map(tag => { return { uid: tag.Uid, Value: tag.Value }; });
    }
    get type() {
        if (this.result) {
            switch (this.result.Group) {
                case 'FusionAttributes':
                    return 'FusionAttribute';
                case 'Reference':
                    return 'ReferenceItemType';
                default:
                    return this.result.Group;
            }
        }
    }

    constructor(private router: Router,
        private shoppingCartService: ShoppingCartService,
        private messagesService: MessagesObservableService,
        private objectStatisticsService: ObjectStatisticsService,
        private ref: ChangeDetectorRef) {
        super();
    }

    ngOnInit() {
        if (CompanySettings != null && CompanySettings.EnableShoppingCart != null && CompanySettings.EnableShoppingCart.toString() == 'true')
            this.showShoppingCart = true;

        this.loadDetails();

    }

  

    private loadDetails() {
        if (this.result.Uid) {
            this.objectStatisticsService.getSearchDetails(this.result.Uid).subscribe(
                result => {
                    this.searchDetails = result;
                    if (this.searchDetails && this.searchDetails.AssetDetail.Status) {
                        this.status = this.searchDetails.AssetDetail.Status;
                        this.showStatus = true;
                    } else {
                        this.showStatus = false;
                    }
                    if (this.searchDetails && this.searchDetails.AssetDetail.Path) {
                        this.formattedPath = this.formatPath(this.searchDetails.AssetDetail.Path);
                        this.showPath = true;
                    } else {
                        this.showPath = false;
                    }
                    this.ref.markForCheck();
                }
            );  
        }

    }

    private formatPath(Path: string): string {
        let res = Path;
        if (Path[0] == "[") {
            res = res.substr(1, Path.length - 1);
        }
        if (Path[Path.length - 1] == "]") {
            res = res.substr(0, Path.length - 2);
        }
        res = res.replace(/(\]\.\[)+/g, " / ");

        return res;
    }

    private navigateLink() {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(this.result.Url));
    }

    private add() {
        var type = this.result.ID.toString().split('|')[0];
        var id = this.result.ID.toString().split('|')[1];
        this.shoppingCartService.addShoppingCartItem(this.type, +id, 1)
            .subscribe(r => this.showMessageForResult(this.messagesService, r));
    }
};