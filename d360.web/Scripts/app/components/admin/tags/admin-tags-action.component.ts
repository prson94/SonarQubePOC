import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { TagType } from '../../../models/tag.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-admin-tags-action',
    templateUrl: 'admin-tags-action.component.html'
})

export class AdminTagsActionComponent implements OnInit  {
    @Input() selectedTags: TagType[] = [];


    @Output() onDelete: EventEmitter<any> = new EventEmitter();;
    @Output() onConsolidate: EventEmitter<any> = new EventEmitter();
    @Output() onTagToggle: EventEmitter<boolean> = new EventEmitter();;

    private isEnabled: boolean = false;

    ngOnInit() {
        this.isEnabled = CompanySettings['EnableTagging'] == 'true' ? true : false;
    }

    onDeleteClick() {
        this.onDelete.emit();
    }
    onConsolidateClick() {
        this.onConsolidate.emit();
    }

    changeTagStatusConfirmation($event) {
        this.isEnabled = $event;
        this.changeStatus();
    }


    changeStatus() {
        this.onTagToggle.emit(this.isEnabled);
    }
};
