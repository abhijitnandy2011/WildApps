import { Component, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgIf } from '@angular/common';

import { KENDO_GRID, GridComponent, RowClassArgs, SelectableSettings } from '@progress/kendo-angular-grid';
import { KENDO_CONTEXTMENU, ContextMenuComponent } from '@progress/kendo-angular-menu';
import { KENDO_TOOLBAR } from '@progress/kendo-angular-toolbar';
import { KENDO_BUTTONS } from '@progress/kendo-angular-buttons';
import { KENDO_CARD } from '@progress/kendo-angular-layout';
import { KENDO_DIALOGS } from '@progress/kendo-angular-dialog';

import { FolderObjectResponse } from '../../models/folder-object-response';
import { FolderObject } from '../../models/folder-object';

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
import { FolderService } from '../../services/folder.service';
import { GetFolderContentsResponse } from '../../models/get-folder-contents-response';

@Component({
  selector: 'app-file-mgr',
  imports: [KENDO_GRID, KENDO_TOOLBAR, KENDO_BUTTONS, KENDO_CONTEXTMENU, KENDO_CARD, KENDO_DIALOGS, NgIf, FormsModule],
  templateUrl: './file-mgr.component.html',
  styleUrl: './file-mgr.component.css',
})
export class FileMgrComponent {
  // View queries for the grid & its context menu
  @ViewChild('grid') public grid: GridComponent;
  @ViewChild('gridmenu') public gridContextMenu: ContextMenuComponent;

  //title = 'kendo-angular-app';

  // The folderId of the folder whose contents are being currently displayed
  private folderId: string;
  // The folders contents grid data - this is the main grid
  public gridData: FolderObject[] = [];
  // Grid selection settings
  public selectableSettings: SelectableSettings;
  // Right click menu items
  public menuItems: MenuItems[] = menuItems;
  private rowIndex: any;
  private rowId: string;
  private contextItem: any;

  // Grid toolbar items
  //public showCommandRow: boolean = false;
  public addFolderIcon: SVGIcon = folderAddIcon;
  public addFileIcon: SVGIcon = fileAddIcon;
  public renameIcon: SVGIcon = groupBoxIcon;
  public deleteIcon: SVGIcon = tableAlignRemoveIcon;
  public fileDownloadIcon: SVGIcon = downloadIcon;
  public fileUploadIcon: SVGIcon = uploadIcon;

  // Add folder and file, input dialog
  public opened: boolean = false;
  public folderObjectName: string = 'New Folder';
  public folderObjectTypePrompt: string = 'folder';

  // ctor
  constructor(private route: ActivatedRoute, private folderService: FolderService) {
    this.setSelectableSettings();
  }

  ngOnInit(): void {
    // Accessing route parameters
    this.folderId = this.route.snapshot.params['path'];
    console.log(`FileMgrComponent:ngOnInit: Folder ID:${this.folderId}`);
    if (!this.folderId) {
      this.folderId = '1';
    }
    // Accessing query parameters
    //const param = this.route.snapshot.queryParams['param'];
    // Accessing route data
    //const data = this.route.snapshot.data;
    this.getFolderObjects();
  }

  // Get the folder contents(which are folder objects i.e. folder, file or link)
  getFolderObjects(): void {
    this.gridData = [];
    this.folderService.getSysObjects(this.folderId).subscribe(
      (result: any) => {
        //debugger;
        //this.responseObjList = result;
        console.log(result);
        if (result instanceof GetFolderContentsResponse) {
          /// ^^^^^^ this is FAILING TODO
          result.folderObjects.forEach((res: FolderObjectResponse) => {
            this.gridData.push({
              ID: res.id,
              ObjName: res.name,
              ObjType: res.objectType == 1 ? 'Folder' : 'File',
              LastModifiedDate: res.createdDate,
              Link: res.openUrl,
            });
          });
        }
      },
      (error) => console.log('FileMgrComponent:ngOnInit: Error during API call', error),
      () => console.log('FileMgrComponent:ngOnInit: getSysObjects() completed')
    );
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
    this.openNewFolderObjectDialog('folder');
  }

  public onAddFileClick(): void {
    console.log('on click');
    this.openNewFolderObjectDialog('file');
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

  public onCellClick(e: any): void {
    if (e.type === 'contextmenu') {
      const originalEvent = e.originalEvent;
      originalEvent.preventDefault();

      this.grid.closeRow(this.rowIndex);

      this.rowIndex = e.rowIndex;
      this.rowId = e.dataItem.Id;
      this.contextItem = e.dataItem;
      //this.showCommandRow = false;

      this.gridContextMenu.show({
        left: originalEvent.pageX,
        top: originalEvent.pageY,
      });
    }
  }

  public closeNewFolderObjectDialog(status: string) {
    console.log('closed:', status, ',', this.folderObjectName);
    this.opened = false;
    if (status == 'ok') {
      //this.createFolderObject(this.folderObjectTypePrompt, this.folderObjectName);
      if (this.folderObjectTypePrompt == 'folder') {
        this.folderService.createSubFolder(this.folderId, this.folderObjectName, 'rw').subscribe(
          (result: any) => {
            console.log(result);
            if (result.message == 'success') {
              this.getFolderObjects();
            }
          },
          (error) =>
            console.log('FileMgrComponent:closeNewFolderObjectDialog: Error during API call createSubFolder:', error),
          () => console.log('FileMgrComponent:closeNewFolderObjectDialog: createSubFolder() completed')
        );
      } else {
        this.folderService.createFile();
      }
    }
  }

  public openNewFolderObjectDialog(objectType: string) {
    if (objectType == 'folder') {
      this.folderObjectName = 'NewFolder';
      this.folderObjectTypePrompt = 'folder';
    } else {
      this.folderObjectName = 'NewFile';
      this.folderObjectTypePrompt = 'file';
    }
    this.opened = true;
  }
}
