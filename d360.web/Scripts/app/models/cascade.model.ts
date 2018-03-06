export class CascadingChange {
    parentListItemId: string;
    fieldTypeId: number;

    constructor(fieldTypeId: number, parentListItemId: string) {
        this.parentListItemId = parentListItemId;
        this.fieldTypeId = fieldTypeId;        
    }
}
