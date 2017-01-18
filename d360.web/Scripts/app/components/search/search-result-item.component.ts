import { Component, Input, ChangeDetectionStrategy} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchFullResult } from '../../models/search-result.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-search-result-item',
    template: `       
                <div class="search-res-container">
                    <h4 class="search-result-name"><a (click)="navigateLink()" class="search-result-link" [innerHtml]="result?.Name"></a></h4>
                    <p class="search-result-desc" *ngIf="result?.Description" [innerHtml]="result.Description"></p>
                    <h5 class="search-result-attributes"><span *ngIf="result?.Type">Category: <em class="result-category" [innerHtml]="result?.Type"></em>&nbsp;&nbsp;</span>Type: <em class="result-type">{{displayType}}</em></h5>
                </div>        
                `,    
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class SearchResultItemComponent extends BaseComponent  {
    @Input() result: SearchFullResult;

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

    constructor(private router: Router) {
        super();
    }

    private navigateLink() {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(this.result.Url));
    }    
};