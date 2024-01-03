import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";
import { AssetsIndex } from ".";

const routes: Routes = [
  { path: '', component: AssetsIndex }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AssetsRouter { }
