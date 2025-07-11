import { HttpInterceptorFn } from '@angular/common/http';
import { LS_KEYNAME_USERINFO } from '../settings/app.settings';

export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  const userJSONStr = localStorage.getItem(LS_KEYNAME_USERINFO);
  if (userJSONStr) {
    const user = JSON.parse(userJSONStr);
    const accessToken = user.accessToken;
    const newReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${accessToken}`,
      },
    });
    return next(newReq);
  }
  return next(req);
};
