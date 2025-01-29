import { Component, ViewChild } from '@angular/core';

import { KENDO_GRID, GridComponent } from '@progress/kendo-angular-grid';
import {
  KENDO_CONTEXTMENU,
  ContextMenuComponent,
} from '@progress/kendo-angular-menu';
import { KENDO_TOOLBAR } from '@progress/kendo-angular-toolbar';
import { KENDO_BUTTONS } from '@progress/kendo-angular-buttons';

import { SysObject } from '../../models/sysobject';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';

import { MenuItems, menuItems } from '../../data/menu-items';

@Component({
  selector: 'app-file-mgr',
  imports: [KENDO_GRID, KENDO_TOOLBAR, KENDO_BUTTONS, KENDO_CONTEXTMENU],
  templateUrl: './file-mgr.component.html',
  styleUrl: './file-mgr.component.css',
})
export class FileMgrComponent {
  @ViewChild('gridmenu') public gridContextMenu: ContextMenuComponent;
  @ViewChild('grid') public grid: GridComponent;

  title = 'kendo-angular-app';
  private path: string;

  responseObjList: any[] = [];

  public gridData: SysObject[] = [];

  public menuItems: MenuItems[] = menuItems;
  private rowIndex: any;
  private rowId: string;
  private contextItem: any;

  public showCommandRow: boolean = false;

  constructor(private route: ActivatedRoute, private http: HttpClient) {}

  ngOnInit(): void {
    // Accessing route parameters
    this.path = this.route.snapshot.params['path'];
    console.log(`path:${this.path}`);
    if (!this.path) {
      this.path = '1';
    }
    // Accessing query parameters
    //const param = this.route.snapshot.queryParams['param'];
    // Accessing route data
    //const data = this.route.snapshot.data;

    this.getSysObjects();
  }

  getSysObjects() {
    this.http
      .get(`https://localhost:7131/api/Folder?id=${this.path}`)
      .subscribe((result: any) => {
        //debugger;
        //this.responseObjList = result;

        console.log(result);

        result.forEach((res: any) => {
          this.gridData.push({
            ID: res.id,
            ObjName: res.name,
            LastModifiedDate: res.createdDate,
            Link: res.openUrl, //`/files/${res.id}`,
          });
        });
      });
  }

  public onCellClick(e: any): void {
    if (e.type === 'contextmenu') {
      const originalEvent = e.originalEvent;
      originalEvent.preventDefault();

      this.grid.closeRow(this.rowIndex);

      this.rowIndex = e.rowIndex;
      this.rowId = e.dataItem.Id;
      this.contextItem = e.dataItem;
      this.showCommandRow = false;

      this.gridContextMenu.show({
        left: originalEvent.pageX,
        top: originalEvent.pageY,
      });
    }
  }
}
