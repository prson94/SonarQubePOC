import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { TagService } from '../../../services/tag.service';
import { AdminBaseComponent } from '../admin-base.component'
import { TagType } from '../../../models/tag.model';

@Component({
    selector: 'd3s-admin-tags-action',
    templateUrl: 'admin-tags-action.component.html'
})

export class AdminTagsActionComponent  {
    @Input() selectedTags: TagType[] = [];


    @Output() onDelete: EventEmitter<any> = new EventEmitter();;
    @Output() onConsolidate: EventEmitter<any> = new EventEmitter();;


    onDeleteClick() {
        this.onDelete.emit();
    }
    onConsolidateClick() {
        this.onConsolidate.emit();
    }
};
