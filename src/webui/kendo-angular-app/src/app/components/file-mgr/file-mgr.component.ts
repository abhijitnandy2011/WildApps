import { Component, ViewChild } from '@angular/core';

import {
  KENDO_GRID,
  GridComponent,
  RowClassArgs,
  SelectableSettings,
} from '@progress/kendo-angular-grid';
import {
  KENDO_CONTEXTMENU,
  ContextMenuComponent,
} from '@progress/kendo-angular-menu';
import { KENDO_TOOLBAR } from '@progress/kendo-angular-toolbar';
import { KENDO_BUTTONS } from '@progress/kendo-angular-buttons';
import { KENDO_CARD } from '@progress/kendo-angular-layout';

import { SysObject } from '../../models/sysobject';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';

import { MenuItems, menuItems } from '../../data/menu-items';

import {
  SVGIcon,
  folderAddIcon,
  fileAddIcon,
  tableAlignRemoveIcon,
  groupBoxIcon,
  uploadIcon,
  downloadIcon,
} from '@progress/kendo-svg-icons';

@Component({
  selector: 'app-file-mgr',
  imports: [
    KENDO_GRID,
    KENDO_TOOLBAR,
    KENDO_BUTTONS,
    KENDO_CONTEXTMENU,
    KENDO_CARD,
  ],
  templateUrl: './file-mgr.component.html',
  styleUrl: './file-mgr.component.css',
})
export class FileMgrComponent {
  @ViewChild('gridmenu') public gridContextMenu: ContextMenuComponent;
  @ViewChild('grid') public grid: GridComponent;

  title = 'kendo-angular-app';
  private path: string;

  public selectableSettings: SelectableSettings;

  public gridData: SysObject[] = [];

  public menuItems: MenuItems[] = menuItems;
  private rowIndex: any;
  private rowId: string;
  private contextItem: any;

  public showCommandRow: boolean = false;

  public addFolderIcon: SVGIcon = folderAddIcon;
  public addFileIcon: SVGIcon = fileAddIcon;
  public renameIcon: SVGIcon = groupBoxIcon;
  public deleteIcon: SVGIcon = tableAlignRemoveIcon;
  public fileDownloadIcon: SVGIcon = downloadIcon;
  public fileUploadIcon: SVGIcon = uploadIcon;

  constructor(private route: ActivatedRoute, private http: HttpClient) {
    this.setSelectableSettings();
  }

  public setSelectableSettings(): void {
    this.selectableSettings = {
      checkboxOnly: false,
      mode: 'multiple',
      drag: false,
      metaKeyMultiSelect: true,
    };
  }

  public onAddFolderClick(): void {
    console.log('on click');
  }

  public onAddFileClick(): void {
    console.log('on click');
  }

  public onRenameClick(): void {
    console.log('on click');
  }

  public onDeleteClick(): void {
    console.log('on click');
  }

  public onUploadFileClick(): void {
    console.log('on click');
  }

  public onDownloadFileClick(): void {
    console.log('on click');
  }

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
            ObjType: res.objectType == 1 ? 'Folder' : 'File',
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

  public rowCallback = (context: RowClassArgs) => {
    return 'gold';
  };
}
