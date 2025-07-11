import { Component, EventEmitter, inject, Output } from '@angular/core';
import { APP_NAME, LS_KEYNAME_USERINFO } from '../settings/app.settings';
import { KENDO_APPBAR } from '@progress/kendo-angular-navigation';
import { KENDO_BADGECONTAINER } from '@progress/kendo-angular-indicators';
import { KENDO_ICONS } from '@progress/kendo-angular-icons';
import { KENDO_LAYOUT } from '@progress/kendo-angular-layout';
import { SVGIcon, menuIcon, bellIcon, paletteIcon } from '@progress/kendo-svg-icons';
import { Router } from '@angular/router';

@Component({
  selector: 'app-header',
  imports: [KENDO_APPBAR, KENDO_ICONS, KENDO_BADGECONTAINER, KENDO_LAYOUT],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  @Output() public toggle = new EventEmitter();

  public appName = APP_NAME;

  public menuIcon: SVGIcon = menuIcon;
  public bellIcon: SVGIcon = bellIcon;
  public kendokaAvatar = 'assets/github.svg';

  router = inject(Router);

  public onButtonClick(): void {
    this.toggle.emit();
  }

  logout() {
    localStorage.removeItem(LS_KEYNAME_USERINFO);
    this.router.navigateByUrl('login');
  }
}
