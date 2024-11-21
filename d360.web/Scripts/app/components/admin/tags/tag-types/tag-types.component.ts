import { Component, EventEmitter, Output, ViewChild } from '@angular/core';
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
    showEditor = false;

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
            this.tagTypes = this.tagTypesCopy.filter((tag) => tag.Value.toLowerCase()
                .includes(val.toLowerCase()))
        }
        else {
            this.tagTypes = this.tagTypesCopy;
        }

    }

    saveTagType(formData: { item: { Value: string } }) {
        const { item } = formData;
        this.ts.addNewTagType(item.Value).subscribe((res) => {

            this.tagTypes = [{ uid: res.uid, Value: res.Value }, ...this.tagTypes];
            this.showEditor = false;
        })
    }

    closeEditor() {
        this.showEditor = false;
    }

    openEditor() {
        this.showEditor = true;
    }
}
