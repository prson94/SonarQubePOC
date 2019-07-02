import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../http-interceptors/govern-post-request.interceptor";
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';

import { HomeSearchComponent} from './home-search.component'
import { SearchResultsComponent } from './search-results.component'
import { SearchResultItemComponent } from './search-result-item.component'
import { SearchComponent } from './search.component'
import { SearchAutocompleteListComponent } from './search-autocomplete-list.component';
import { SearchInputComponent } from './search-input.component';

import { SearchRoutingModule} from './search.routes';

import {    
    InputTextModule,        
    ButtonModule,
    DropdownModule,
    CheckboxModule,                  
    MultiSelectModule,    
    TooltipModule,    
    PaginatorModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,                
        HttpClientModule,
        RouterModule,

        SearchRoutingModule,

        //primeng         
        InputTextModule,                  
        ButtonModule,
        DropdownModule,
        CheckboxModule,                        
        MultiSelectModule,        
        TooltipModule,        
        PaginatorModule,
        SharedModule,

        //d3s        
        CoreModule,
        TilesModule,
    ],
    declarations: [
        HomeSearchComponent,
        SearchResultsComponent,
        SearchResultItemComponent,
        SearchComponent,
        SearchAutocompleteListComponent,    
        SearchInputComponent,    
    ],
    exports: [
        HomeSearchComponent,                                      
    ],
    providers: [        
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class SearchModule { }