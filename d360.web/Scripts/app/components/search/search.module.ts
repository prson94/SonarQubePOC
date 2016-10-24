import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { D3SSharedModule } from '../shared/shared.module';
import { CoreModule } from '../shared/core.module';

import { HomeSearchComponent} from './home-search.component'
import { SearchResultsComponent } from './search-results.component'
import { SearchResultItemComponent } from './search-result-item.component'
import { SearchComponent } from './search.component'
import { SearchAutocompleteListComponent } from './search-autocomplete-list.component';
import { SearchInputComponent } from './search-input.component';

import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,
    MenuModule,
    MenubarModule,
    AccordionModule,
    SelectButtonModule,
    AutoCompleteModule,
    MultiSelectModule,
    SpinnerModule,    
    TooltipModule,
    DragDropModule,
    PaginatorModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,                
        HttpModule,
        RouterModule,

        //primeng 
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        DataTableModule,        
        ButtonModule,
        DropdownModule,
        CheckboxModule,        
        MenuModule,
        MenubarModule,
        AccordionModule,
        SelectButtonModule,        
        MultiSelectModule,
        SpinnerModule,        
        TooltipModule,        
        PaginatorModule,
        SharedModule,

        //d3s
        D3SSharedModule,
        CoreModule,

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
        SearchResultsComponent,
        SearchResultItemComponent,
        SearchComponent,
        SearchAutocompleteListComponent,        
        SearchInputComponent,
    ]
})
export class SearchModule { }