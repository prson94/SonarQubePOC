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

    @Output('onTagTypeSelected') onTagTypeSelected = new EventEmitter<TagTypesViewModel>();
    @ViewChild('searchinput', { static: true }) searchInput: SearchFieldComponent;
    
    tagTypes: TagTypesViewModel[] = [];
    tagTypesCopy: TagTypesViewModel[] = [];
    isLoading = true;
    searchText$ = "";
    showEditor = false;
    showTagTypeActions = false;
    selectedTag: TagTypesViewModel = null;
    modalTitle: string = 'Add Tag Type';
    formSubmitAction: string = 'add';
    selectedAction = 'add';
    showDeleteModal = false;
    theDeleteCallback: unknown;

    constructor(private ts: TagTypesService) { }

    ngOnInit() {
        this.isLoading = true;
        if (this.tagTypesCopy.length === 0) {
            this.ts.getAllTagTypes().subscribe((data) => {
                this.tagTypes = data
                this.tagTypesCopy = data;
            });
        }
        this.isLoading = false;
        this.theDeleteCallback = this.deleteTagType.bind(this);
    }

    onClick(e: Event){
        e.stopPropagation();
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
        if (this.formSubmitAction === 'add') {
            this.ts.addNewTagType(item.Value).subscribe((res) => {

                this.tagTypes = [{ uid: res.uid, Value: res.Value }, ...this.tagTypes];
                this.showEditor = false;
                this.onTagTypeSelected.emit({...res});
            });
            return;
        }
        if (this.formSubmitAction === 'edit') {
            this.ts.updateTagType(item.Value, this.selectedTag.uid).subscribe(() => {
                const index = this.tagTypes.findIndex((tag) => tag.uid === this.selectedTag.uid);
                this.tagTypes[index].Value = item.Value;
                this.showEditor = false;
            });
            this.onTagTypeSelected.emit(null);
            return;
        }

    }

    closeEditor() {
        this.showEditor = false;
    }

    openEditor(obj: { mTitle: string, action: string }) {
        this.showTagTypeActions = false;
        const { mTitle, action } = obj;
        this.modalTitle = mTitle;
        this.formSubmitAction = action;
        this.showEditor = true;

    }

    openDeleteModal() {
        this.showDeleteModal = true;
        this.showTagTypeActions = false;
    }

    loadTags(tagType: TagTypesViewModel) {
        this.onTagTypeSelected.emit(tagType);
    }

    selectRow(row: TagTypesViewModel) {
        this.selectedTag = row;
        this.showTagTypeActions = !this.showTagTypeActions;
    }

    deleteTagType() {
        this.ts.deleteTagType(this.selectedTag.uid).subscribe(() => {
            this.tagTypes = this.tagTypes.filter((tag) => tag.uid !== this.selectedTag.uid);
            this.showDeleteModal = false;
            this.onTagTypeSelected.emit(null);
        });


    }

}
