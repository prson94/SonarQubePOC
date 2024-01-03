import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AppRouter } from './app.routing';
import { CommonModule } from '@angular/common';
import { BrowserModule } from '@angular/platform-browser';

import { BaseComponent } from './base';
import { AnonymousRoot } from './anonymous';
import { AuthorizedRoot } from './authorized';

@NgModule({
  declarations: [
    BaseComponent,
    AnonymousRoot,
    AuthorizedRoot
  ],
  imports: [
    CommonModule,
    BrowserModule,
    RouterModule,
    HttpClientModule,
    AppRouter
  ],
  providers: [],
  bootstrap: [BaseComponent]
})
export class AppModule { }
