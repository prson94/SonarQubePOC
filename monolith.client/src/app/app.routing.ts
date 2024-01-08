import { RouterModule, Routes } from "@angular/router";
import { NgModule } from "@angular/core";
import { AuthorizedRoot } from "./authorized";
import { AnonymousRoot } from "./anonymous";
import { NotFoundPage } from "./errors/not-found";

const routes: Routes = [
  {
    path: 'login',
    component: AnonymousRoot,
    loadChildren: () => import('./login/_module').then(m => m.LoginModule)
  },
  {
    path: '',
    component: AuthorizedRoot,
    children: [
      { path: 'asset', loadChildren: () => import('./asset/_module').then(m => m.AssetModule) },
      { path: 'assets', loadChildren: () => import('./assets/_module').then(m => m.AssetsModule) },
      { path: '', loadChildren: () => import('./home/_module').then(m => m.HomeModule) }
    ]
  },
  { path: '**', component: NotFoundPage }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRouter { }
