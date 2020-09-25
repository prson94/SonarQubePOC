import { Component, OnInit, ChangeDetectionStrategy, HostListener } from '@angular/core';
import { FieldsObservableService } from '../../services/fieldsObservable.service';
import { FieldTypeAPIModelFieldCondition } from '../shared/controls/field-condition-grid/field-condition-grid.models';


@Component({
    selector: 'gallery-field-condition-grid',
    templateUrl: './gallery.field-condition-grid.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }

        `
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService]
})

export class GalleryFieldConditionGridComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<ig-popup-menu [items]="items"></ig-popup-menu>';
    protected isLoading1: boolean = true;
    protected isLoading2: boolean = false;

    assetTypeUid: string = '4a35d6dc-2ece-4676-adc1-b83cb469b2aa';
    fields: FieldTypeAPIModelFieldCondition[];

    private operators = [
        { label: 'Is', value: 'Is' },
        { label: 'Is not', value: 'Is not' },
        { label: 'In', value: 'In' },
        { label: 'Not In', value: 'Not In' },
        { label: 'Contains', value: 'Does not contain' }
    ];

    constructor(
        private fieldsService: FieldsObservableService
    ) {

    }


    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "items", Type: "Array<PopupMenuItem>", Description: "Array of menu items", Default: "Empty []" });


        this.fieldsService.getFieldsV2(this.assetTypeUid, null, null).subscribe(res => {
            this.fields = res as FieldTypeAPIModelFieldCondition[];
            this.fields.forEach(f => {
                f.Operators = JSON.parse(JSON.stringify(this.operators));
            });
        });
    }
}
