import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';

import { AssetRouter } from './_routing';
import { AssetIndex } from './index';
import { CommonModule } from '@angular/common';

@NgModule({
  declarations: [
    AssetIndex
  ],
  imports: [
    CommonModule,
    HttpClientModule,
    AssetRouter
  ],
  providers: []
})
export class AssetModule { }
