import { Component, ViewChild } from '@angular/core';
import { SearchFieldComponent } from '../../../shared/controls/search-field/search-field.component';
import { TagTypesViewModel } from './tag-types.model';
import { TagTypesService } from './tag-types.service';

@Component({
    selector: 'd3s-tag-types',
    templateUrl: './tag-types.component.html',
    styleUrls: ['./tag-types.component.less'],
})
export class TagTypesPanelComponent {

    @ViewChild('searchinput', { static: true }) searchInput: SearchFieldComponent;

    tagTypes: TagTypesViewModel[] = [];
    tagTypesCopy: TagTypesViewModel[] = [];
    isLoading = true;
    searchText$ = "";

    constructor(private ts: TagTypesService) { }

    ngOnInit() {
        this.ts.getAllTagTypes().subscribe((data) => {
            this.tagTypes = data
            this.tagTypesCopy = data;
        });


        this.isLoading = false;
    }

    filterTagTypes(val: string) {
        if (val?.length > 0) {
            this.tagTypes = this.tagTypes.filter((tag) => tag.Value.toLowerCase()
                .includes(val.toLowerCase()))
        }
        else {
            this.tagTypes = this.tagTypesCopy;
        }

    }
}
