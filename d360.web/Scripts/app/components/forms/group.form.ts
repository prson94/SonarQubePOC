
import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { SelectItem } from 'primeng/primeng';
import { Group, GroupEditorModel, GroupSearchResultModel, ResourceGroup } from '../../models/group.model';
import { FormEvents, FormHelper, JsonResult } from '../../models/form.model';
import { GroupService } from '../../services/group.service';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-group-form',
    templateUrl: 'scripts/app/components/forms/group.form.html',
    providers: [GroupService],
})

export class GroupForm implements OnInit, OnChanges, FormEvents { 
    @Input() id: number;
    @Input() title: string = "Edit Group";
    @Output() onComplete = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onError = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();

    private model: GroupEditorModel;
    private isLoading = false;
    private initialItem: GroupEditorModel;

    constructor(private groupService: GroupService) {
        this.model = new GroupEditorModel();
        this.model.group = new Group();
    }

    ngOnInit() {
        this.initialItem = _.cloneDeep(this.model);

        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();
                this.initialItem = _.cloneDeep(this.model);
            }
        }
    }

    private load(): void {
        this.isLoading = true;
        this.groupService.getGroup(this.id)
            .then(d => {
                this.model = d;
                //TODO: primeng does not currently support optgroups, I'm ignoring them here
                FormHelper.mapSelectItems(this.model.resourceList);
                this.onLoadComplete.emit(null);
                this.isLoading = false;
                console.log(this.model);
            });
        this.onLoadComplete.emit(null);
    }

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private save(): void {
        this.isLoading = true;
        if (this.id > 0) {
            this.groupService.putGroup(this.model.group)
                .then(r => {
                    if (r.type == 'confirm') {
                        this.onSuccess.emit(this.model.group);
                    } else if (r.type == 'error') {
                        this.onError.emit(r);
                    }
                    this.isLoading = false;
                    this.onComplete.emit(null);
                });
        } else {
            this.groupService.postGroup(this.model.group)
                .then(r => {
                    if (r.type == 'confirm') {
                        this.onSuccess.emit(r);
                    } else if (r.type == 'error') {
                        this.onError.emit(r);
                    }
                    this.isLoading = false;
                    this.onComplete.emit(null);
                });
        }
    }
}
