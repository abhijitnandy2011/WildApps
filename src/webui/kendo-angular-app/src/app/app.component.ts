import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { KENDO_GRID, CellClickEvent } from '@progress/kendo-angular-grid';
import { KENDO_TOOLBAR } from '@progress/kendo-angular-toolbar';
import { KENDO_BUTTONS } from '@progress/kendo-angular-buttons';

import { SysObject } from './models/product';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, KENDO_GRID, KENDO_TOOLBAR, KENDO_BUTTONS],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
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
        });
      });
  }

  public cellClickHandler(args: CellClickEvent): void {
    //debugger;
    // if (!args.isEdited) {
    //   args.sender.editCell(
    //     args.rowIndex,
    //     args.columnIndex,
    //     this.createFormGroup(args.dataItem)
    //   );
    // }
  }
}
