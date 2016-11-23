import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
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
    MenuModule,        
    SelectButtonModule,    
    MultiSelectModule,    
    TooltipModule,    
    PaginatorModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,                
        HttpModule,
        RouterModule,

        SearchRoutingModule,

        //primeng         
        InputTextModule,                  
        ButtonModule,
        DropdownModule,
        CheckboxModule,        
        MenuModule,                
        SelectButtonModule,        
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
    ]
})
export class SearchModule { }