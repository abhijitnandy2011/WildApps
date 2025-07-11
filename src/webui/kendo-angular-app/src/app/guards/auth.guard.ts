import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { LS_KEYNAME_USERINFO } from '../settings/app.settings';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  var user = localStorage.getItem(LS_KEYNAME_USERINFO);
  if (user === null) {
    router.navigateByUrl('login');
    return false;
  }
  return true;
};
