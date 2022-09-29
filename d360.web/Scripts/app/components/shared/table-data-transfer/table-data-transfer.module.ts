import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableDataTransferComponent } from './table-data-transfer.component';
import { SearchFieldModule } from '../controls/search-field/search-field.component';
import { TableModule } from 'primeng/table';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from '../../../directives/ig-button-directive';



@NgModule({
  declarations: [
    TableDataTransferComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
    SearchFieldModule,
    TableModule,
    SharedGridPagingInfoModule,
    ButtonModule,
  ],
  exports: [
    TableDataTransferComponent
  ]
})
export class TableDataTransferModule { }
