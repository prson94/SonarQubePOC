import { NgModule } from '@angular/core';

import { HomeRouter } from './_routing';
import { HomeIndex } from './index';
import { CommonModule } from '@angular/common';

@NgModule({
  declarations: [
    HomeIndex
  ],
  imports: [
    CommonModule,
    HomeRouter
  ],
  providers: []
})
export class HomeModule { }
