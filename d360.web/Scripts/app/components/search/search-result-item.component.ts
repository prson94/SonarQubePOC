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
import { MenuItem } from 'primeng/api';

declare var CompanySettings;

@Component({
    selector: 'd3s-search-result-item',
    templateUrl: './search-result-item.component.html',
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
    private displayInfopopup: boolean = false;

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

    private showBadges(): boolean {
        return this.showStatus || (this.searchDetails && this.searchDetails.Scores.length > 0)
    }

    private showInfo() {
        this.displayInfopopup = true;
    }

    private getCardMenuItems(): MenuItem[] {
        var menu: MenuItem[] = [
            { label: 'More Information', command: (event) => { this.showInfo() } },
        ];
        if (this.result.Uid && CompanySettings.LineageVersion == 3 && ['Reference', 'Resource', 'Group', 'Grammatic type', 'Attribute', 'Fusion'].indexOf(this.result.Group) == -1) {
            menu.push({
                label: 'View Visualization',
                command: (event) => { this.navigateVisualization(); }
            });
        }
        if (this.showShoppingCart && ['Synonym', 'Attribute', 'Grammatic type'].indexOf(this.result.Group) == -1) {
            menu.push({
                label: '',
                icon: 'fa fa-cart-plus',
                command: (event) => { this.add(); }
            });
        }
        return menu;
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

    private navigateVisualization() {
        var url = '/sidebar/visualization/browser/'+this.result.Uid
        this.router.navigateByUrl(url);
    }

    private add() {
        var type = this.result.ID.toString().split('|')[0];
        var id = this.result.ID.toString().split('|')[1];
        this.shoppingCartService.addShoppingCartItem(this.type, +id, 1)
            .subscribe(r => this.showMessageForResult(this.messagesService, r));
    }

    private getDataForPreview() {
        return {
            DisplayName: this.result.DisplayName,
            TypeName: this.result.Type,
            Description: this.result.Description,
            AssetID: this.result.ID,
            UID: this.result.Uid
        }
    }
};