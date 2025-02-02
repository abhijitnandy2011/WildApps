import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CreateFolderRequest } from '../models/create-folder-request';

@Injectable({
  providedIn: 'root',
})
export class FolderService {
  constructor(private http: HttpClient) {}

  // Get the folders contents using API
  getSysObjects(folderId: string) {
    var obsv = this.http.get(`https://localhost:7131/api/Folder?id=${folderId}`);
    return obsv;
  }

  createSubFolder(parentFolderId: string, folderName: string, attributes: string) {
    const requestObj: CreateFolderRequest = {
      parentFolderId: parseInt(parentFolderId),
      subFolderName: folderName,
      attributes: attributes,
    };
    return this.http.post(`https://localhost:7131/api/Folder/createUsingID`, requestObj);
  }

  createFile() {}
}
