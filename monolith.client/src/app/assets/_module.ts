import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';

import { AssetsRouter } from './_routing';
import { AssetsIndex } from './index';
import { CommonModule } from '@angular/common';

@NgModule({
  declarations: [
    AssetsIndex
  ],
  imports: [
    CommonModule,
    HttpClientModule,
    AssetsRouter
  ],
  providers: []
})
export class AssetsModule { }
