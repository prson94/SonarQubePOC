import { Component, Input, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchFullResult } from '../../models/search-result.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { ShoppingCartService } from '../../services/shopping-cart.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { ObjectStatistics } from '../../models/object-statistics.model';
import { TagService } from '../../services/tag.service';
import { Tag, TagItem } from '../../models/tag.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-search-result-item',
    template: `       
                <div class="search-res-container">
                        <div class="col s8">
                            <h4 class="search-result-name">
                                <i *ngIf="result?.Icon" class="folder-icon fa {{result?.Icon}}"></i><span *ngIf="result?.ImageUrl" class="folder-icon"><img [src]="result.ImageUrl" /></span> <a (click)="navigateLink()" class="search-result-link" [innerHtml]="result?.Name"></a>
                            </h4>
                            <h5 class="search-result-attributes">
                                <span class="result-type">{{displayType}}</span>
                                <span *ngIf="result?.Type">
                                        &nbsp; <i class="fa fa-angle-right"></i> &nbsp;<span class="result-category" [innerHtml]="result?.Type"></span>
                                </span>
                             </h5>
                            <p class="search-result-desc" *ngIf="result?.Description" [innerHtml]="result.Description"></p>
                        </div>
                        <div class="col s4 search-result-details">
                            <div class="search-result-details-line"> 
                                <span #badge *ngIf="statistics && statistics.Score;else noScore" class="d3s-icon large-icon clickable"
                                      title="{{lastCalculatedMessage()}}"
                                      [ngClass]="{
                                                            'bad':scoreBetween(0,49),
                                                            'ok':scoreBetween(50,89),
                                                            'good':scoreBetween(90,1000)
                                                        }">
			                        <d3s-dynamic-percentage [percentage]="statistics?.Score"></d3s-dynamic-percentage>
			                        <span class="text">{{statistics?.Score}}%</span>
		                        </span>
		                        <ng-template #noScore>
			                        <span #noScoreBadge title="Governance Score not yet calculated" class="d3s-icon large-icon clickable">
				                        <d3s-dynamic-percentage [percentage]="0"></d3s-dynamic-percentage>
				                        <span class="text">N/A</span>
			                        </span>
		                        </ng-template>
		                        <span *ngIf="showStatus" class="d3s-icon large-icon" [style.background-color]="getCertificationStatusColor(status)">
			                        <i class="fa fa-certificate"></i>
			                        <span class="text">{{status}}</span>
		                        </span>
                                <button *ngIf="showShoppingCart && result.Group != 'Synonym' && result.Group != 'Attribute'" class="button icon" (click)="add()">
                                    <i class="fa fa-cart-plus"></i>
                                </button>
                                <span class="d3s-icon med-icon light">
                                    <i class="fa fa-info-circle"></i>
                                </span>
                            </div>
                            <span class="spacer"></span>
                            <div class="search-result-details-line">
                                <d3s-tag-view [data]="tags"></d3s-tag-view>
                            </div>
                        </div>
                </div>        
                `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ShoppingCartService]
})

export class SearchResultItemComponent extends BaseComponent implements OnInit {
    @Input() result: SearchFullResult;
    private tags: TagItem[] = [];
    private statistics: ObjectStatistics;
    private lastCalculatedDate: number;
    private showStatus: boolean = true;
    private status: string = 'certified';
    showShoppingCart = false;

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

    constructor(private router: Router, private shoppingCartService: ShoppingCartService, private messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {
        if (CompanySettings != null && CompanySettings.EnableShoppingCart != null && CompanySettings.EnableShoppingCart.toString() == 'true')
            this.showShoppingCart = true;

        this.statistics = new ObjectStatistics();
        this.statistics.Score = 90
        this.statistics.ScoreLast = "2019-03-10T00:00:00.000Z";
        this.loadDetails();
    }

    private loadDetails() {
        this.tags.push({ Uid: 123, Value: 'tag1' });
        this.tags.push({ Uid: 123, Value: 'tag2' });
        this.tags.push({ Uid: 123, Value: 'tag3' });
        this.tags.push({ Uid: 123, Value: 'tag4' });
        this.tags.push({ Uid: 123, Value: 'tag5' });
        this.tags.push({ Uid: 123, Value: 'tag5' });

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
        if (this.statistics) {
            return this.statistics.Score >= start && this.statistics.Score <= end;
        }
    }

    getCertificationStatusColor(status: string) {
        status = status.toLowerCase().trim();

        switch (status) {
            case 'draft':
                return '#BBBBBB';
            case 'certified':
                return '#3f9d40';
            case 'under review':
                return '#e2792a';
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
        if (!this.statistics) {
            return "Governance Score not yet calculated";
        }
        var diff = new Date(Date.now() - Date.parse(this.statistics.ScoreLast));

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