import { Routes } from "@angular/router";
import { AuthorizedRoot } from "./authorized";
import { AnonymousRoot } from "./anonymous";
import { NotFoundPage } from "./errors/not-found";
import { CanLoadAsAuthorized } from './_shared/guards/CanLoadAsAuthorized';

export const routes: Routes = [
  {
    path: 'login',
    component: AnonymousRoot,
    children: [
      { path: '', loadComponent: () => import('./login/index').then(m => m.LoginIndex) }
    ]
  },
  {
    path: '',
    canActivate: [CanLoadAsAuthorized],
    component: AuthorizedRoot,
    children: [
      { path: 'asset', loadComponent: () => import('./asset/index').then(m => m.AssetIndex) },
      { path: 'assets', loadComponent: () => import('./assets/index').then(m => m.AssetsIndex) },
      { path: '', loadComponent: () => import('./home/index').then(m => m.HomeIndex) }
    ]
  },
  {
    path: 'forbidden',
    component: AnonymousRoot,
    children: [
      { path: '', loadComponent: () => import('./errors/forbidden').then(m => m.ForbiddenPage) }
    ]
  },
  {
    path: '**',
    component: AnonymousRoot,
    children: [
      { path: '', loadComponent: () => import('./errors/not-found').then(m => m.NotFoundPage) }
    ]
  },
];
