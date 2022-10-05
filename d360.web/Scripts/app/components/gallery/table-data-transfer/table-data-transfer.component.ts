import { Component, OnInit } from '@angular/core';
import { OutputEvents, Property } from '../interface/gallery.interface';
import { EVENTS, ITEMS_FROM_SOURCE, ITEMS_FROM_SOURCE_ADVANCED, ITEMS_FROM_TARGET, ITEMS_FROM_TARGET_ADVANCED, PROPERTIES, SAMPLE_USAGE } from './table-data-transfer.data';

@Component({
  selector: 'gallery-table-data-transfer',
  templateUrl: './table-data-transfer.component.html',
  styleUrls: ['./table-data-transfer.component.less']
})
export class GalleryTableDataTransferComponent implements OnInit {
  itemsFromSource: any[] = ITEMS_FROM_SOURCE;
  itemsFromTarget: any[] = ITEMS_FROM_TARGET;
  itemsFromSourceAdvanced: any[] = ITEMS_FROM_SOURCE_ADVANCED;
  itemsFromTargetAdvanced: any[] = ITEMS_FROM_TARGET_ADVANCED;
  sampleUsage: string = SAMPLE_USAGE;
  properties: Property[] = PROPERTIES;
  events: OutputEvents[] = EVENTS;
  

  constructor() { }

  ngOnInit(): void {
  }

}
