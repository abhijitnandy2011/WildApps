import { Routes } from '@angular/router';
import { NotePadComponent } from './components/note-pad/note-pad.component';
import { FileMgrComponent } from './components/file-mgr/file-mgr.component';
import { RSheetComponent } from './components/r-sheet/r-sheet.component';

export const routes: Routes = [
  {
    path: '',
    component: FileMgrComponent,
    pathMatch: 'full',
  },
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
];
