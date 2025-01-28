import { Routes } from '@angular/router';
import { NotePadComponent } from './components/note-pad/note-pad.component';
import { FileMgrComponent } from './components/file-mgr/file-mgr.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'FileMgrComponent',
    pathMatch: 'full',
  },
  {
    path: 'files',
    component: FileMgrComponent,
  },
  {
    path: 'notepad',
    component: NotePadComponent,
  },
];
