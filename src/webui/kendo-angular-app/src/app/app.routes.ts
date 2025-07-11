import { Routes } from '@angular/router';
import { NotePadComponent } from './components/note-pad/note-pad.component';
import { FileMgrComponent } from './components/file-mgr/file-mgr.component';
import { RSheetComponent } from './components/r-sheet/r-sheet.component';
import { LoginComponent } from './components/login/login.component';
import { LayoutComponent } from './components/layout/layout.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
  {
    path: 'login',
    component: LoginComponent,
  },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'files',
        component: FileMgrComponent,
      },
      {
        path: 'files/:path',
        component: FileMgrComponent,
      },
      {
        path: 'notepad',
        component: NotePadComponent,
      },
      {
        path: 'notepad/:id',
        component: NotePadComponent,
      },
      {
        path: 'rsheet',
        component: RSheetComponent,
      },
      {
        path: 'rsheet/:id',
        component: RSheetComponent,
      },
    ],
  },
];
