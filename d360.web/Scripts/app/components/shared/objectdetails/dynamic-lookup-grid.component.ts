import { Component, Input, Output, OnInit} from '@angular/core';
import { Column } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../../models/lookup.model';
import { LookupGrid, GridColumn, GridField, GridFilterColumn } from '../../../models/grid-definition.model';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { BaseComponent } from '../base.component';
import { DetailField } from '../../../models/object-detail.model';
import { ObjectDetailService } from '../../../services/object-detail.service';


@Component({
    selector: 'd3s-dynamic-lookup-grid',    
    templateUrl: './dynamic-lookup-grid.component.html',
    providers: [ObjectDetailService]
})

export class DynamicLookupGridComponent extends BaseComponent implements OnInit {
    @Input() data: LookupGrid;
    @Input() field: DetailField;
    @Input() hideFooter = false;
    @Input() hideHeader = false;
    @Input() hideFilter = true;

    isComplex = false;
    showSimpleFilter = true;

    visibleColumns: GridFilterColumn[] = [];

    get globalFilterFields(): string[] {
        return this.visibleColumns.map(c => c.datafield);
    }

    constructor(private router: Router, private objectDetailService: ObjectDetailService) {
        super();
    }
    
    ngOnInit() {
        
        this.isComplex = (this.data.Fields.find(f => f.name == 'Url') == null);

        //do this on init to avoid binding to function call
        this.data.Columns.forEach(c => {
            c.type = this.columnDataType(c);  
            if (c.type == 'number') {
                this.data.Values.forEach(v => {
                    v[c.datafield] = this.formatAsNumber(v[c.datafield]);
                });
            }
            if (c.type == 'string' || c.type == 'preview' || c.type == 'lookup' || c.type=='html') {
                this.data.Values.forEach(v => {
                    if (v[c.datafield] === null) {
                        v[c.datafield] = ''; //prevent IE from displaying 'null'
                    }
                });
            }
        });

        this.data.Columns.filter(c => c.type == 'hidden').forEach(c => {
            let i = this.data.Columns.find(i => i.datafield == c.text);
            if (i) {
                i.type = 'preview';
            }
        });

        this.visibleColumns = this.data.Columns.filter(c => c.type != 'hidden');
    }

    private formatAsNumber(val): string {
        return val != '' && val != null ? Number(val).toLocaleString() : "";
    }

    private getHeaderStyle():string {
        if (this.hideHeader) return "hidenHeader";
        return "";
    }

    private columnDataType(column: GridFilterColumn): string {
        var fields = this.data.Fields.filter(x => x.name == column.datafield);

        if (column.type == 'preview')
            return 'preview';
        if ((column.datafield == 'Name' || column.datafield == 'TextPath') && !this.isComplex)
            return 'tooltip';
        if (fields.length > 0)
            return fields[0].type;
        return 'string';
    }

    navigate(url: string) {
        //TODO: should attempt to generate dynamically by object/objectid eventually
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url)); 
    }

    export() {
        this.objectDetailService.getLookupGridExport(this.field.LookupObjectType, this.field.LookupObjectID, this.field.LookupFieldTypeID, this.field.LookupType);
    }

    customSort(e: any) {        
        let field = e.field;
        let direction = e.order;
        let col = this.visibleColumns.find(c => c.datafield == field);
        let type = col == null ? '' : col.type;

        this.data.Values = this.data.Values.slice().sort((a, b) => {
            let fa = a[field];
            let fb = b[field];

            switch (type) {
                case 'number':
                    let na: number = +fa;
                    let nb: number = +fb;

                    if (na == null || isNaN(na))
                        na = -Infinity;
                    if (nb == null || isNaN(nb))
                        nb = -Infinity;

                    return ((na > nb) ? 1 : (na < nb) ? -1 : 0) * direction;
                case 'date':
                case 'datetime':
                    let da: number = Date.parse(fa);
                    let db: number = Date.parse(fb);

                    if (da == null || isNaN(da))
                        da = new Date(null).getTime();
                    if (db == null || isNaN(db))
                        db = new Date(null).getTime();

                    return ((da > db) ? 1 : (da < db) ? -1 : 0) * direction;
                default:
                    return ((fa > fb) ? 1 : (fa < fb) ? -1 : 0) * direction;
            }
        });
        
    }
}



