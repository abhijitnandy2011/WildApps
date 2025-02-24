import { FolderObjectResponse } from './folder-object-response';

export class GetFolderContentsResponse {
  code: number;
  folderObjects: FolderObjectResponse[];
  id: number;
  message: string;
}
