import {Input, Output, Component, OnChanges, SimpleChange} from '@angular/core';
import {DetailRow, DetailField, DetailModel, DetailFieldType} from '../../../models/object-detail.model';
import {ObjectDetailService} from '../../../services/object-detail.service';
import {LookupGrid} from '../../../models/grid-definition.model';
import {MessagesService} from '../../../services/messages.service';

declare var CompanySettings;

@Component({
    selector: 'object-detail',
    templateUrl: './object-detail.component.html',
    providers: [ObjectDetailService]
})


export class ObjectDetailComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;

    private isLoading = false;
    DetailFieldType = DetailFieldType;

    private TaxonomyTypeName = 'ArtifactTaxonomyType';
    private TaxonomyTypeNodeName = 'ArtifactTaxonomyTypeNodes';

    private categories: Category[] = new Array<Category>();

    rows = new Array<DetailRow>();

    constructor(private objectDetailService: ObjectDetailService, protected messagesService: MessagesService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'objectType') {
                this.objectType = changes['objectType'].currentValue;
            }
            if (p == 'objectID') {
                this.objectID = changes['objectID'].currentValue;
            }
        }

        this.load();
    }

    public load(): void {
        if (this.objectType && this.objectID) {
            this.isLoading = true;

            this.objectDetailService.getObjectDetail(this.objectID, this.objectType).subscribe(
                data => {
                    this.rows = data.rows;
                    this.categories = [];

                    this.rows.forEach(r => {
                            if (r.Category && this.categories.find(c => c.name == r.Category) == null) {
                                this.categories.push(new Category(r.Category));
                            }

                            r.FirstColumnFields.forEach(
                                f => {
                                    this.setDetailFieldType(f);

                                    if (f.FieldName == this.TaxonomyTypeName) {
                                        f.Name = CompanySettings.ArtifactType_TaxonomyTypeID;
                                    }

                                    if (f.FieldName == this.TaxonomyTypeNodeName) {
                                        f.Name = CompanySettings.ArtifactType_TaxonomyTypeIDNodes;
                                    }

                                    if (f.Type == DetailFieldType.Lookup) {
                                        this.objectDetailService.getLookupGrid(f.LookupGridUrl).subscribe(
                                            i => {
                                                f.Data = i;

                                                if (!f.Data || !f.Data.Values || f.Data.Values.length == 0) {
                                                    f.Type = DetailFieldType.None;
                                                    r.FirstColumnFields.splice(r.FirstColumnFields.indexOf(f), 1);
                                                }
                                            }
                                        );
                                    }

                                });

                            r.FirstColumnFields = r.FirstColumnFields.filter(f => f.Type != DetailFieldType.None);

                            r.SecondColumnFields.forEach(
                                s => {
                                    this.setDetailFieldType(s);

                                    if (s.FieldName == this.TaxonomyTypeName) {
                                        s.Name = CompanySettings.ArtifactType_TaxonomyTypeID;
                                    }

                                    if (s.FieldName == this.TaxonomyTypeNodeName) {
                                        s.Name = CompanySettings.ArtifactType_TaxonomyTypeIDNodes;
                                    }

                                    if (s.Type == DetailFieldType.Lookup) {
                                        this.objectDetailService.getLookupGrid(s.LookupGridUrl).subscribe(
                                            i => {
                                                s.Data = i;

                                                if (!s.Data || !s.Data.Values || s.Data.Values.length == 0) {
                                                    s.Type = DetailFieldType.None;
                                                    r.SecondColumnFields.splice(r.SecondColumnFields.indexOf(s), 1);
                                                }
                                            }
                                        );
                                    }
                                }
                            );

                            r.SecondColumnFields = r.SecondColumnFields.filter(f => f.Type != DetailFieldType.None);
                        }
                    );

                    let displayRows = this.rows.filter(r => r.Category == null && ((r.FirstColumnFields && r.FirstColumnFields.length > 0) || (r.SecondColumnFields && r.SecondColumnFields.length > 0)));

                    for (let i = 0; i < this.categories.length; i++) {
                        let items = this.rows.filter(r => r.Category == this.categories[i].name);
                        this.categories[i].rows = [];
                        for (let j of items) {
                            if ((j.FirstColumnFields && j.FirstColumnFields.length > 0) || (j.SecondColumnFields && j.SecondColumnFields.length)) {
                                this.categories[i].rows.push(j);
                            }
                        }
                    }
                    this.rows = displayRows;
                    this.loadCategory();
                    this.isLoading = false;
                }
            );
        }
    }


    private setDetailFieldType(field: DetailField) {
        field.Type = DetailFieldType.Field;
        if ((field.Value == null || field.Value == '') && field.ShowIfEmpty == false)
            field.Type = DetailFieldType.None;
        if (field.TooltipContext != null) {
            if (field.Value != null && field.Value != '')
                field.Type = DetailFieldType.Tooltip;
            else
                field.Type = DetailFieldType.None;
        }

        if (field.LookupGridUrl != null) {
            field.Type = DetailFieldType.Lookup;
        }
    }

    private loadCategory() {
        this.categories.forEach(c => {
            var rcount = c.rows.length;
            c.rows.forEach(r => {
                let fcount = r.FirstColumnFields.length;
                r.FirstColumnFields.forEach(f => {
                    if (f.Type == DetailFieldType.Lookup) {
                        if (!f.Data || !f.Data.Values || f.Data.Values.length == 0) {
                            c.hasData = true;
                        }
                        fcount--;

                        if (fcount <= 0)
                            rcount--;

                        if (rcount <= 0)
                            c.loaded = true;
                    } else {
                        if (f.Type != DetailFieldType.None)
                            c.hasData = true;
                        fcount--;
                        if (fcount <= 0)
                            rcount--;
                        if (rcount <= 0)
                            c.loaded = true;
                    }
                });
            });
        });

    }
}

class Category {
    constructor(name: string) {
        this.name = name;
    }

    loaded = false;
    hasData = false;
    name: string;
    rows = [];
}
