///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Component, OnInit, Input} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/index';
import { SearchFullResult } from '../../models/search-result.model';

@Component({
    selector: 'd3s-search-result-item',
    template: `       
                <div class="search-res-container">
                    <h4 class="search-result-name"><a (click)="navigateLink()" class="search-result-link" [innerHtml]="result?.Name"></a></h4>
                    <p class="search-result-desc" [innerHtml]="result?.Description"></p>
                    <h5 class="search-result-attributes">Category: <em class="result-category">{{result?.Type}}</em>&nbsp;&nbsp;Type: <em class="result-type">{{result?.Group}}</em></h5>
                </div>        
                `,
    providers: [SearchService], 
})

export class SearchResultItemComponent extends BaseComponent  {
    @Input() result: SearchFullResult;

    constructor(private router: Router) {
        super();
    }

    private navigateLink() {
        this.router.navigateByUrl(this.convertUrl(this.result));
    }

    public convertUrl(item: SearchFullResult): string {
        switch (item.Group.toUpperCase()) {
            case 'ARTIFACT':
                return item.Url.replace('#/artifacts', '/a/artifact');
            case 'USERS':            
                return item.Url.replace('#/resources', '/a/resource');            
        }

        return item.Url.replace('#', '/a');
    }
};