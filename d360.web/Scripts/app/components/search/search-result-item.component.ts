///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Component, OnInit, Input} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/index';
import { SearchFullResult } from '../../models/search-result.model';

@Component({
    selector: 'd3s-search-result-item',
    template: `       
                <div class="search-res-container">
                    <h4 class="search-result-name"><a [href]="result?.Url" class="search-result-link" [innerHtml]="result?.Name"></a></h4>
                    <p class="search-result-desc" [innerHtml]="result?.Description"></p>
                    <h5 class="search-result-attributes">Category: <em class="result-category">{{result?.Type}}</em>&nbsp;&nbsp;Type: <em class="result-type">{{result?.Group}}</em></h5>
                </div>        
                `,
    providers: [SearchService],
})

export class SearchResultItemComponent extends BaseComponent  {
    @Input() result: SearchFullResult;
        
};