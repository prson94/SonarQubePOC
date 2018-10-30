export class TableColumn {
    //constructor();
    constructor(properties?: any) {
        this.field = properties && properties.field || '';
        this.header = properties && properties.header || '';
        this.sortable = properties && properties.sortable || false;
        this.filterable = properties && properties.filterable || false;
        this.filterMatchMode = properties && properties.filterMatchMode || 'contains';
        this.datatype = properties && properties.datatype || 'text';
    }

    field: string = '';
    header: string = '';
    sortable: boolean = false;
    filterable: boolean = false;
    filterMatchMode: string = 'contains';
    datatype: string = 'text';
}