import { Component, OnInit } from '@angular/core';
import { OutputEvents, Property } from '../interface/gallery.interface';
import { EVENTS, ITEMS_FROM_SOURCE, ITEMS_FROM_SOURCE_ADVANCED, ITEMS_FROM_TARGET, ITEMS_FROM_TARGET_ADVANCED, PROPERTIES, SAMPLE_USAGE } from './table-data-transfer.data';

/*global $localize*/

@Component({
  selector: 'gallery-table-data-transfer',
  templateUrl: './table-data-transfer.component.html',
  styleUrls: ['./table-data-transfer.component.less']
})
export class GalleryTableDataTransferComponent implements OnInit {
  itemsFromSource: any[] = ITEMS_FROM_SOURCE.sort((a, b) => a.Title?.localeCompare(b.Title));
  itemsFromTarget: any[] = ITEMS_FROM_TARGET;
  itemsFromSourceAdvanced: any[] = ITEMS_FROM_SOURCE_ADVANCED.sort((a, b) => a.Title?.localeCompare(b.Title));
  itemsFromTargetAdvanced: any[] = ITEMS_FROM_TARGET_ADVANCED;
  sampleUsage: string = SAMPLE_USAGE;
  properties: Property[] = PROPERTIES;
  events: OutputEvents[] = EVENTS;

  basicSourceTableTitle = $localize`Source Table Title`;
  basicTargetTableTitle = $localize`Target Table Title`;
  advancedSourceTableTitle = $localize`Advanced Source Table Title`;
  advancedTargetTableTitle = $localize`Advanced Target Table Title`;
  advancedEmptyTargetTableMessage = $localize`No selected items`;
  

  constructor() { }

  ngOnInit(): void {
  }

}
