// Folder contents API response, folder object
export class FolderObjectResponse {
  id: number;
  name: string;
  description: string;
  path: string;
  objectType: number;
  objectTypeStr: string;
  attributes: string;
  openUrl: string;
  iconUrl: string;
  createdBy: string;
  createdDate: string;
  lastUpdatedBy: string;
  lastUpdatedDate: string;
}
