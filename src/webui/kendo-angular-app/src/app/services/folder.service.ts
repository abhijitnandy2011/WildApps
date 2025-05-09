import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CreateFolderRequest } from '../models/create-folder-request';
import { API_URL } from '../settings/app.settings';

@Injectable({
  providedIn: 'root',
})
export class FolderService {
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
}
