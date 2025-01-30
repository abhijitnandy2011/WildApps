import { Component, EventEmitter, Output } from '@angular/core';
import { KENDO_APPBAR } from '@progress/kendo-angular-navigation';
import { KENDO_BADGECONTAINER } from '@progress/kendo-angular-indicators';
import { KENDO_ICONS } from '@progress/kendo-angular-icons';
import { KENDO_LAYOUT } from '@progress/kendo-angular-layout';
import {
  SVGIcon,
  menuIcon,
  bellIcon,
  paletteIcon,
} from '@progress/kendo-svg-icons';

@Component({
  selector: 'app-header',
  imports: [KENDO_APPBAR, KENDO_ICONS, KENDO_BADGECONTAINER, KENDO_LAYOUT],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  @Output() public toggle = new EventEmitter();
  public menuIcon: SVGIcon = menuIcon;
  public bellIcon: SVGIcon = bellIcon;
  public kendokaAvatar = 'assets/github.svg';

  public onButtonClick(): void {
    this.toggle.emit();
  }
}
