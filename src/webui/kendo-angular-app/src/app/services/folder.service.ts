import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CreateFolderRequest } from '../models/create-folder-request';
import { API_URL, LS_KEYNAME_USERINFO } from '../settings/app.settings';
import { firstValueFrom, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class FolderService {
  tokenExpired$: Subject<boolean> = new Subject<boolean>();
  tokenRecvd$: Subject<boolean> = new Subject<boolean>();

  constructor(private http: HttpClient) {}

  // Get the folders contents using API
  getSysObjects(folderId: string) {
    var obsv = this.http.get(`${API_URL}/Folder?id=${folderId}`);
    return obsv;
  }

  createSubFolder(parentFolderId: string, folderName: string, attributes: string) {
    const requestObj: CreateFolderRequest = {
      parentFolderId: parseInt(parentFolderId),
      subFolderName: folderName,
      attributes: attributes,
    };
    return this.http.post(`${API_URL}/Folder/createUsingID`, requestObj);
  }

  createFile() {}

  //------------ DEVEL -----------------
  async get_DEV_AuthenticatedUser(): Promise<boolean> {
    try {
      var result = await firstValueFrom(this.http.get('https://dummyjson.com/auth/me'));
      console.log('get_DEV_AuthenticatedUser:result:', result);
      return true;
    } catch (error: any) {
      console.log('ERROR:get_DEV_AuthenticatedUser:', error);
      return false;
    }
  }

  async get_DEV_RefreshTokens() {
    try {
      const userJSONStr = localStorage.getItem(LS_KEYNAME_USERINFO);
      if (userJSONStr) {
        const user = JSON.parse(userJSONStr);
        const refreshToken = user.refreshToken;
        var result: any = await firstValueFrom(
          this.http.post('https://dummyjson.com/auth/refresh', {
            refreshToken: refreshToken,
            expiresInMins: 1,
          })
        );
        console.log('get_DEV_RefreshTokens:result:', result);
        user.accessToken = result.accessToken;
        user.refreshToken = result.refreshToken;
        await localStorage.setItem(LS_KEYNAME_USERINFO, JSON.stringify(user));
        this.tokenRecvd$.next(true);
      } else {
        console.error('ERROR:get_DEV_RefreshTokens:No user info present locally');
      }
    } catch (error: any) {
      console.log('ERROR:get_DEV_RefreshTokens:', error);
    }
  }
}
