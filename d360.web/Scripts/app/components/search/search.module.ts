import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';

import { HomeSearchComponent} from './home-search.component'
import { HeroSearchInputComponent } from './hero-search-input';
import { SearchResultsComponent } from './search-results.component'
import { SearchResultItemComponent } from './search-result-item.component'
import { SearchComponent } from './search.component'
import { SearchAutocompleteListComponent } from './search-autocomplete-list.component';
import { SearchInputComponent } from './search-input.component';
import { DynamicPercentageModule } from '../shared/small-widgets/dynamic-percentage/dynamic-percentage-module';
import { PaginatorModule } from '../shared/small-widgets/paginator/paginator-bar-module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SearchRoutingModule} from './search.routes';

import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { SharedModule } from 'primeng/shared';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { TypeaheadSearchModule } from '../shared/search/typeahead-search.component';


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
        DynamicPercentageModule,
        SharedDynamicGridEditorModule,
        PaginatorModule,
        TypeaheadSearchModule
    ],
    declarations: [
        HomeSearchComponent,
        SearchResultsComponent,
        SearchResultItemComponent,
        SearchComponent,
        SearchAutocompleteListComponent,    
        SearchInputComponent,  
        HeroSearchInputComponent
    ],
    exports: [
        HomeSearchComponent,    
        SearchInputComponent,
        HeroSearchInputComponent,
    ],
    providers: [        
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class SearchModule { }