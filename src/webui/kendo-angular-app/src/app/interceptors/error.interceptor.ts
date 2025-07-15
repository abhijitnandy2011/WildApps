import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { FolderService } from '../services/folder.service';
import { inject } from '@angular/core';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const folderService = inject(FolderService);
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        const willContinue = confirm('Do you want to continue the session?');
        if (willContinue) {
          folderService.tokenExpired$.next(true);
        }
      }
      return throwError(() => error);
    })
  );
};
