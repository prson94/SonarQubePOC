import { Component, Input, ChangeDetectionStrategy, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchFullResult } from '../../models/search-result.model';
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
                        <span #badge *ngIf="scoreAndStatus && scoreAndStatus.Score;else noScore" class="d3s-icon large-icon"
                              title="{{lastCalculatedMessage()}}"
                              [ngClass]="{
                                                    'fail':scoreBetween(0,49),
                                                    'ok':scoreBetween(50,89),
                                                    'good':scoreBetween(90,1000)
                                                }">
			                <d3s-dynamic-percentage [percentage]="scoreAndStatus?.Score"></d3s-dynamic-percentage>
			                <span class="text">{{scoreAndStatus?.Score}}%</span>
		                </span>
		                <span *ngIf="showStatus" class="d3s-icon large-icon" [style.background-color]="getCertificationStatusColor(status)">
			                <i class="fa fa-certificate"></i>
			                <span class="text">{{status}}</span>
		                </span>
                        <button *ngIf="showShoppingCart && result.Group != 'Synonym' && result.Group != 'Attribute'" class="button icon" (click)="add()">
                            <i class="fa fa-cart-plus"></i>
                        </button>
                        <span *ngIf="result.Uid" class="d3s-icon med-icon light">
                            <d3s-preview-tooltip [uid]="result.Uid" icon="info-circle"></d3s-preview-tooltip>
                        </span>
                    </span>
                    <span class="category">
                        {{displayType}}<span *ngIf="result?.Type"><i class="fa fa-angle-right"></i><span class="category" [innerHtml]="result?.Type"></span></span>
                    </span>
                    <span class="description" *ngIf="result?.Description" [innerHtml]="result.Description"></span>
                    <div *ngIf="result?.Tags"  class="tags">
                        <d3s-tag-view [data]="parseTagResult(result?.Tags)"></d3s-tag-view>
                    </div>      
                </div>        
                `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ShoppingCartService, ObjectStatisticsService]
})

export class SearchResultItemComponent extends BaseComponent implements OnInit {
    @Input() result: SearchFullResult;
    private lastCalculatedDate: number;
    private showStatus: boolean = false;
    private status: string;
    showShoppingCart = false;
    private obj: string;
    private objID: number;
    private scoreAndStatus: any;
    get displayType() {
        if (this.result) {
            switch (this.result.Group) {
                case 'Artifact':
                    return 'Glossary';
                case 'Synonym':
                    return 'Grammatic Type';
                default:
                    return this.result.Group
            }
        }
    }
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
            this.objectStatisticsService.getScoreAndStatus(this.result.Uid).subscribe(
                result => {
                    this.scoreAndStatus = result;
                    if (this.scoreAndStatus && this.scoreAndStatus.Status) {
                        this.status = this.scoreAndStatus.Status; 
                        this.showStatus = true;
                    } else {
                        this.showStatus = false;
                    }
                    this.ref.markForCheck();
                }
            );  
        }

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

    scoreBetween(start, end) {
        if (this.scoreAndStatus && this.scoreAndStatus.Score) {
            return this.scoreAndStatus.Score >= start && this.scoreAndStatus.Score <= end;
        }
    }

    getCertificationStatusColor(status: string) {
        status = status.toLowerCase().trim();

        switch (status) {
            case 'draft':
                return '#6c7884';
            case 'certified':
                return '#00853e';
            case 'under review':
                return '#FFB000';
            default:
                //custom status, we need to generate a color
                let hash = 0;
                for (let i = 0; i < status.length; i++) {
                    hash = status.charCodeAt(i) + ((hash << 5) - hash);
                    hash = hash & hash;
                }
                return `hsl(${(hash * 2) % 360}, 70%, 70%)`;
        }
    }

    private lastCalculatedMessage() {
        if (!this.scoreAndStatus.EffectiveDate) {
            return "Governance Score not yet calculated";
        }
        var diff = new Date(Date.now() - Date.parse(this.scoreAndStatus.EffectiveDate));

        var years = diff.getUTCFullYear() - 1970;

        if (years > 0) return "Governance Score last calculated " + years + " years ago.";

        var months = diff.getUTCMonth();

        if (months > 0) return "Governance Score last calculated " + months + " months ago.";

        var days = diff.getUTCDate() - 1;

        if (days > 0) return "Governance Score last calculated " + days + " days ago.";

        var hours = diff.getUTCHours();

        if (hours > 0) return "Governance Score last calculated " + hours + " hours ago.";

        var minutes = diff.getUTCMinutes();

        if (minutes > 0) return "Governance Score last calculated " + minutes + " minutes ago.";

        return "Governance Score last calculated a few seconds ago.";
    }
};