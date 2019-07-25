import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { TagService } from '../../../services/tag.service';
import { AdminBaseComponent } from '../admin-base.component'
import { TagType } from '../../../models/tag.model';

@Component({
    selector: 'd3s-admin-tags-consolidate',
    templateUrl: 'admin-tags-consolidate.component.html'
})

export class AdminTagsConsolidateComponent implements OnInit {
    @Input() tags: TagType[] = [];
    private selected: TagType;

    @Input() modalTitle: string = '';
    @Input() isModalVisible: boolean = false;

    @Input() callback: Function;
    @Output() onCancel = new EventEmitter();

    ngOnInit() {

    }

    public consolidate(): void {
        let parentUid: string = this.selected.uid;
        let childrenUids: string[] = [];
        this.tags.forEach(t => {
            if (t.uid != parentUid)
                childrenUids.push(t.uid);
        });
        this.callback(parentUid, childrenUids);
    }

    public cancel(): void {
        this.onCancel.emit(null);
    }

    selectItem(tag: TagType) {
        this.selected = tag;
    }

};
