import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";
import { HomeIndex } from ".";

const routes: Routes = [
  { path: '', component: HomeIndex }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class HomeRouter { }
