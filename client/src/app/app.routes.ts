import { Routes } from '@angular/router';


  export const routes: Routes = [
    { path: '', redirectTo: 'upload', pathMatch: 'full' },
    { path: 'upload', loadComponent: () => import('./upload-image/upload-image.component').then(c => c.UploadImageComponent) },
  ]
