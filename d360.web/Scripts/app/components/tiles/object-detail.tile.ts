///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { DetailRow, DetailField, DetailModel, IObjectDetailService } from '../../models/object-detail.model';
import { ObjectDetailService } from '../../services/object-detail.service';

declare var CompanySettings;

@Component({
    selector: 'object-detail',
    templateUrl: 'scripts/app/components/tiles/object-detail.tile.html',
    providers: [ObjectDetailService]
})


export class ObjectDetailTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;

    private isLoading = false;

    private TaxonomyTypeName = 'ArtifactTaxonomyType';
    private TaxonomyTypeNodeName = 'ArtifactTaxonomyTypeNodes';


    rows = new Array<DetailRow>();
    columns: number;

    constructor(private objectDetailService: ObjectDetailService) {
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

    private load(): void {


        if (this.objectType && this.objectID) {
            this.isLoading = true;
            this.objectDetailService.getObjectDetail(this.objectID, this.objectType)
                .then(data => {
                    this.rows = data.rows;
                    this.columns = data.columns;

                    this.rows.forEach(r => {
                        r.FirstColumnFields.forEach(f => {
                            if (f.FieldName == this.TaxonomyTypeName) {
                                f.Name = CompanySettings.ArtifactType_TaxonomyTypeID;
                            }
                            if (f.FieldName == this.TaxonomyTypeNodeName) {
                                f.Name = CompanySettings.ArtifactType_TaxonomyTypeIDNodes;
                            }
                        });
                        r.SecondColumnFields.forEach(s => {
                            if (s.FieldName == this.TaxonomyTypeName) {
                                s.Name = CompanySettings.ArtifactType_TaxonomyTypeID;
                            }
                            if (s.FieldName == this.TaxonomyTypeNodeName) {
                                s.Name = CompanySettings.ArtifactType_TaxonomyTypeIDNodes;
                            }
                        });
                    });
                    this.isLoading = false;
                    console.log(data);
                });
        }
    }
}
