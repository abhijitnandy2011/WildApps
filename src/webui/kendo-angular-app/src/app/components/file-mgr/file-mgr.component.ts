import { Component } from '@angular/core';

import { KENDO_GRID } from '@progress/kendo-angular-grid';
import { KENDO_TOOLBAR } from '@progress/kendo-angular-toolbar';
import { KENDO_BUTTONS } from '@progress/kendo-angular-buttons';

import { SysObject } from '../../models/sysobject';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-file-mgr',
  imports: [KENDO_GRID, KENDO_TOOLBAR, KENDO_BUTTONS],
  templateUrl: './file-mgr.component.html',
  styleUrl: './file-mgr.component.css',
})
export class FileMgrComponent {
  title = 'kendo-angular-app';

  responseObjList: any[] = [];

  public gridData: SysObject[] = [];

  constructor(private http: HttpClient) {}

  getSysObjects() {
    this.http
      .get('https://localhost:7131/api/Folder?id=1')
      .subscribe((result: any) => {
        //debugger;
        this.responseObjList = result;

        this.gridData.push({
          ID: result[0].id,
          ObjName: result[0].name,
          LastModifiedDate: result[0].createdDate,
          Link: '/mpm/20',
        });
      });
  }
}
