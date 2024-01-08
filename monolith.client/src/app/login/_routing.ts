import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";
import { LoginIndex } from ".";

const routes: Routes = [
  { path: '', component: LoginIndex }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class LoginRouter { }
