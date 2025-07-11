import { Component } from '@angular/core';

import { NavigationStart, Router, RouterLink, RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../../header/header.component';
import { KENDO_DRAWER, DrawerComponent, DrawerMode, DrawerSelectEvent } from '@progress/kendo-angular-layout';
import { gridIcon, clockIcon } from '@progress/kendo-svg-icons';

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, HeaderComponent, KENDO_DRAWER],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.css',
})
export class LayoutComponent {
  public selected = 'Team';
  public items: Array<any> = [];
  public mode: DrawerMode = 'push';
  public mini = true;

  constructor(private router: Router /*, public msgService: MessageService*/) {
    //this.customMsgService = this.msgService as CustomMessagesService;
  }

  ngOnInit() {
    // Update Drawer selected state when change router path
    this.router.events.subscribe((route: any) => {
      if (route instanceof NavigationStart) {
        this.items = this.drawerItems().map((item) => {
          if (item.path && item.path === route.url) {
            item.selected = true;
            return item;
          } else {
            item.selected = false;
            return item;
          }
        });
      }
    });

    this.setDrawerConfig();

    /*this.customMsgService.localeChange.subscribe(() => {
      this.items = this.drawerItems();
    });*/

    window.addEventListener('resize', () => {
      this.setDrawerConfig();
    });
  }

  public setDrawerConfig() {
    const pageWidth = window.innerWidth;
    if (pageWidth <= 840) {
      this.mode = 'overlay';
      this.mini = false;
    } else {
      this.mode = 'push';
      this.mini = true;
    }
  }

  public drawerItems() {
    return [
      {
        text: 'Files',
        svgIcon: gridIcon,
        path: '/files',
        selected: true,
      },
      {
        text: 'Recent',
        svgIcon: clockIcon,
        path: '/dashboard',
        selected: false,
      },
    ];
  }

  public toggleDrawer(drawer: DrawerComponent): void {
    console.log('Drawer toggled');
    drawer.toggle();
  }

  public onSelect(ev: DrawerSelectEvent): void {
    this.router.navigate([ev.item.path]);
    this.selected = ev.item.text;
  }
}
