import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Policies',
    loadComponent: () => import('./policies/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'policies/new',
    title: 'Upload policy',
    loadComponent: () => import('./policies/upload/upload').then((m) => m.Upload),
  },
  {
    path: 'policies/:id',
    title: 'Policy',
    loadComponent: () => import('./policies/policy-page/policy-page').then((m) => m.PolicyPage),
  },
  { path: '**', redirectTo: '' },
];
