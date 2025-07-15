import { Component, EventEmitter, inject, Output } from '@angular/core';
import { APP_NAME, LS_KEYNAME_USERINFO } from '../settings/app.settings';
import { KENDO_APPBAR } from '@progress/kendo-angular-navigation';
import { KENDO_BADGECONTAINER } from '@progress/kendo-angular-indicators';
import { KENDO_ICONS } from '@progress/kendo-angular-icons';
import { KENDO_LAYOUT } from '@progress/kendo-angular-layout';
import { SVGIcon, menuIcon, bellIcon, paletteIcon } from '@progress/kendo-svg-icons';
import { Router } from '@angular/router';
import { FolderService } from '../services/folder.service';

import { firstValueFrom, Subject, Subscription } from 'rxjs';

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

  private subTokenExpired: Subscription;
  private subTokenRecvd: Subscription;

  constructor(private folderService: FolderService, private router: Router) {
    //this.checkTokenExpired();
    console.log('HeaderComponent:subscribe*******');
    this.subTokenExpired = this.folderService.tokenExpired$.subscribe({
      next: (isTokenExpired) => {
        try {
          if (isTokenExpired) {
            // Refresh the token
            this.folderService.get_DEV_RefreshTokens(); // keep refreshing token
            //this.subTokenExpired.unsubscribe(); // no need to unsubsc here, only after component unloaded
          }
        } catch (error: any) {
          console.log('ERROR:checkTokenExpired:', error);
        }
      },
    });
  }

  // async checkTokenExpired() {
  //   try {
  //     const isTokenExpired = await firstValueFrom(this.folderService.tokenExpired$);
  //     if (isTokenExpired) {
  //       // Refresh the token
  //       this.folderService.get_DEV_RefreshTokens();
  //     }
  //   } catch (error: any) {
  //     console.log('ERROR:checkTokenExpired:', error);
  //   }
  // }

  ngOnDestroy() {
    if (this.subTokenExpired) this.subTokenExpired.unsubscribe(); // Unsubscribe when the component is destroyed
    if (this.subTokenRecvd) this.subTokenRecvd.unsubscribe();
  }

  public onButtonClick(): void {
    this.toggle.emit();
  }

  logout() {
    localStorage.removeItem(LS_KEYNAME_USERINFO);
    this.router.navigateByUrl('login');
  }

  async sendRequest() {
    const success = await this.folderService.get_DEV_AuthenticatedUser();
    if (!success) {
      this.subTokenRecvd = this.folderService.tokenRecvd$.subscribe({
        next: async (isTokenRecvd) => {
          try {
            if (isTokenRecvd) {
              // New token recvd
              const success = await this.folderService.get_DEV_AuthenticatedUser(); // TODO: need to check if success then unsubsc
              if (!success) {
                console.error('ERROR:sendRequest:get_DEV_AuthenticatedUser() failed even after refresh token');
              }
            }
          } catch (error: any) {
            console.log('ERROR:sendRequest:', error);
          }
          setTimeout(() => {
            this.subTokenRecvd.unsubscribe();
            console.log('Unsubscribed subTokenRecvd');
          }, 100); // put it in timeout
        },
      });
    }
  }
}
