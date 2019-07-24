import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';

import {MapSequenceModel, SourceRuleItem, SourceRuleSequence} from '../../../../models/lineage.model';

import {DiagramService} from '../../../../services/diagram.service';
import {PermissionsService} from '../../../../services/permissions.service';

import {BaseComponent} from '../../base.component';
import { MessagesObservableService } from '../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-lineage-source-rule-editor',
    templateUrl: './lineage-source-rule-editor.component.html',
    providers: [DiagramService]
})

export class LineageSourceRuleEditorComponent extends BaseComponent implements OnInit {
    @Input() object: string;
    @Input() objectId: number;

    @Output() onClose = new EventEmitter();
    @Output() onSaveComplete = new EventEmitter();

    model: MapSequenceModel;
    items: SourceRuleItem[] = [];

    isLoading = false;

    constructor(
        private diagramService: DiagramService,
        protected messagesService: MessagesObservableService,
        protected permissionsService: PermissionsService
    ) {
        super();
    }

    ngOnInit() {
        this.load();

        this.permissionsService.getPermissions(this.objectId, this.object).subscribe(
            data => {
                this.permissions = data;
            }
        );
    }

    load() {
        this.isLoading = true;
        this.diagramService.getLineageMapSequence(this.object, this.objectId).subscribe(
            data => {
                this.model = data;

                this.model.Available.forEach(i => {
                    const top = this.items.find(t => t.TargetIntersectID == i.TargetIntersectID);
                    let topItem: any;

                    if (!top) {
                        topItem = {
                            ID: i.ID,
                            Name: i.Target,
                            TargetIntersectID: i.TargetIntersectID,
                            Available: [],
                            Selected: []
                        };

                        this.model.Available.forEach(s => {
                            if (s.TargetIntersectID == topItem.TargetIntersectID) {
                                topItem.Available.push({
                                    MapItemID: s.ID,
                                    SourceIntersectID: s.SourceIntersectID,
                                    Name: s.Source
                                });
                            }
                        });

                        this.model.Referenced.forEach(r => {
                            if (r.TargetIntersectID == topItem.TargetIntersectID) {
                                let sourceName = '';

                                topItem.Available.forEach(a => {
                                    if (r.MapItemID == a.MapItemID) {
                                        sourceName = a.Name;
                                    }
                                });

                                const selectedItem = {
                                    ID: r.ID,
                                    MapItemID: r.MapItemID,
                                    Sequence: r.Sequence,
                                    Contexts: [],
                                    Description: r.Description,
                                    SourceName: sourceName
                                };

                                // Add to the Selected collection.
                                topItem.Selected.push(selectedItem);
                            }
                        });
                        this.items.push(topItem);
                    }
                });

                this.isLoading = false;
            });
    }

    private add(parent: any, item: any) {
        const newItem = {
            ID: 0,
            MapItemID: item.MapItemID,
            Sequence: parent.Selected.length + 1,
            Contexts: null,
            Description: '',
            SourceName: item.Name || '',
            TargetName: parent.Name || '',
        };

        this.setSequenceNumbers();
        parent.Selected.push(newItem);
    }

    private remove(seq: SourceRuleSequence) {
        seq.IsDeleting = true;
        this.setSequenceNumbers();
    }

    close() {
        this.onClose.emit();
    }

    save() {
        const permCreate = this.hasModifyRelationshipsPermissions();
        const permEdit = this.hasModifyRelationshipsPermissions();

        if (!permEdit || !permCreate) {
            return;
        }

        this.isLoading = true;

        this.setSequenceNumbers();
        const model = {Items: []};

        if (this.items == null) {
            this.items = [];
        }

        this.items.forEach(i => {
            i.Selected.forEach(s => {
                model.Items.push(s);
            });
        });

        this.diagramService.postLineageMapSequence(
            this.object,
            this.objectId,
            model
        ).subscribe(
            r => {
                this.showMessageForResult(this.messagesService, r);

                this.onSaveComplete.emit();

                this.isLoading = false;
            }
        );
    }

    private setSequenceNumbers() {
        this.items.forEach(i => {
            if (i.Selected != null) {
                let index = 1;
                for (let s = 0; s < i.Selected.length; s++) {
                    if (i.Selected[s].IsDeleting) {
                        continue;
                    }

                    i.Selected[s].Sequence = index++;
                }
            }
        });
    }

    moveUp(item: SourceRuleItem, seq: SourceRuleSequence) {
        this.setSequenceNumbers();
        const previousIndex = item.Selected.findIndex(i => i == seq) - 1;

        if (previousIndex < 0) {
            return;
        }

        const previous = item.Selected[previousIndex];
        seq.Sequence--;
        previous.Sequence++;

        item.Selected[previousIndex] = seq;
        item.Selected[previousIndex + 1] = previous;
    }

    moveDown(item: SourceRuleItem, seq: SourceRuleSequence) {
        this.setSequenceNumbers();
        const nextIndex = item.Selected.findIndex(i => i == seq) + 1;

        if (nextIndex >= item.Selected.length) {
            return;
        }

        const next = item.Selected[nextIndex];
        seq.Sequence++;
        next.Sequence--;

        item.Selected[nextIndex] = seq;
        item.Selected[nextIndex - 1] = next;
    }
}
