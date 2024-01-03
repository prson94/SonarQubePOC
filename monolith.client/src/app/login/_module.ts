import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';

import { LoginRouter } from './_routing';
import { LoginIndex } from './index';
import { CommonModule } from '@angular/common';

@NgModule({
  declarations: [
    LoginIndex
  ],
  imports: [
    CommonModule,
    HttpClientModule,
    LoginRouter
  ],
  providers: []
})
export class LoginModule { }
