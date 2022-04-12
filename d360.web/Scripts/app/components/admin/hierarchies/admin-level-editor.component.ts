import { Input, Component, EventEmitter, Output } from '@angular/core';
import { LevelsService } from '../../../services/levels.service';
import { HierarchyTypeLevel } from '../../../models/hierarchy-type-level.model';
import { BaseComponent } from '../../shared/base.component';
import * as _ from 'lodash';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-level-editor',
    templateUrl: './admin-level-editor.component.html',
    providers: [LevelsService],
})

export class AdminLevelEditorComponent extends BaseComponent {
    @Input() level: HierarchyTypeLevel;
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() maxDepth: number;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = $localize`Edit`;
    error: any;
    editedLevel: HierarchyTypeLevel;
    levels: number[] = [];

    labelSave = $localize`Save`;
    labelCancel = $localize`Cancel`;
    labelClose = $localize`Close`;

    constructor(
        private levelsService: LevelsService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnInit() {
        if (this.level != undefined)
            this.editedLevel = _.cloneDeep(this.level);
        else {
            this.editedLevel = new HierarchyTypeLevel();
            this.action = $localize`New`;
        }
        this.getUnusedLevels();
    }

    getUnusedLevels() {
        this.isLoading = true;
        this.levelsService.getObjectLevels(this.objectId, this.objectType).subscribe(
            result => {
                this.isLoading = false;

                for (var i = 1; i <= this.maxDepth; i++) {
                    this.levels.push(i);
                }

                for (var i = 0; i < result.length; i++) {
                    //remove the used level
                    let index = this.levels.map((e) => e).indexOf(result[i].Level);

                    this.levels.splice(index, 1);
                }
            }
        );
    }

    onSubmit() {
        this.saveClick.emit({ level: this.editedLevel, action: this.level == null ? "new" : "edit" });
    }

    close() {
        this.closeClick.emit({});
    }
}
