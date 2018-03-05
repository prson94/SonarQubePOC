export class CascadingChange {
    parentListItemId: number;
    fieldTypeId: number;

    constructor(fieldTypeId?: number, parentListItemId?: number) {
        this.parentListItemId = parentListItemId;
        this.fieldTypeId = fieldTypeId;        
    }
}
