import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RoleList } from './role-list';

@NgModule({
  declarations: [
    RoleList
  ],
  imports: [
    CommonModule,
    HttpClientModule,
    RoleList
  ],
  providers: []
})
export class SecurityComponentModule { }
