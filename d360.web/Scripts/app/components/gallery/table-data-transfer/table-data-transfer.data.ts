import { OutputEvents, Property } from "../interface/gallery.interface";

export const ITEMS_FROM_SOURCE: any[] = [
  { Title: "Source 1", ObjectID: 1, Object: 'asset' },
  { Title: "Source 2", ObjectID: 2, Object: 'asset' },
  { Title: "Source 3", ObjectID: 3, Object: 'asset' },
  { Title: "Source 4", ObjectID: 4, Object: 'asset' },
  { Title: "Source 5", ObjectID: 5, Object: 'asset' },
  { Title: "Source 6", ObjectID: 6, Object: 'asset' },
  { Title: "Source 7", ObjectID: 7, Object: 'asset' },
  { Title: "Source 8", ObjectID: 8, Object: 'asset' },
  { Title: "Source 9", ObjectID: 9, Object: 'asset' },
  { Title: "Source 10", ObjectID: 10, Object: 'asset' },
  { Title: "Source 11", ObjectID: 11, Object: 'asset' },
  { Title: "Source 12", ObjectID: 12, Object: 'asset' },
  { Title: "Source 13", ObjectID: 13, Object: 'asset' },
  { Title: "Source 14", ObjectID: 14, Object: 'asset' },
  { Title: "Source 15", ObjectID: 15, Object: 'asset' },
  { Title: "Source 16", ObjectID: 16, Object: 'asset' },
];

export const ITEMS_FROM_TARGET: any[] = [
  { Title: "Target 111", ObjectID: 111, Object: 'asset' },
  { Title: "Target 222", ObjectID: 222, Object: 'asset' },
  { Title: "Target 333", ObjectID: 333, Object: 'asset' },
  { Title: "Target 444", ObjectID: 444, Object: 'asset' },
  { Title: "Target 555", ObjectID: 555, Object: 'asset' },
  { Title: "Target 666", ObjectID: 666, Object: 'asset' },
  { Title: "Target 777", ObjectID: 777, Object: 'asset' },
];


export const SAMPLE_USAGE: string = `
<d3s-table-data-transfer [itemsFromSource]="itemsFromSource"
                         [itemsFromTarget]="itemsFromTarget"
                         [sourceTableTitle]="'Source Table Title'"
                         [targetTableTitle]="'Target Table Title'"></d3s-table-data-transfer>`;

export const PROPERTIES: Property[] = [
  { Name: "itemsFromSource", Type: "any[]", Description: "input data for Source table", Default: "[]" },
  { Name: "itemsFromTarget", Type: "any[]", Description: "input data for Target table", Default: "[]" },
  { Name: "sourceTableTitle", Type: "string", Description: "Source Table Title", Default: "Source Table Title" },
  { Name: "targetTableTitle", Type: "string", Description: "Target Table Title", Default: "Target Table Title" },
];

export const EVENTS: OutputEvents[] = [
  { Name: "targetDataChangeEvent",  Description: "Fires when Target Table Data changes"},
];
