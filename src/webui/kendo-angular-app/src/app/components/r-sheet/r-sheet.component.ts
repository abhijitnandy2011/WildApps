import { Component } from '@angular/core';
import { MPMDataService } from '../../services/mpm-data.service';
import { MPMGetProductInfoResponse } from '../../models/mpm-getproductinfo-response';
import { MPMGetRangeInfoResponse } from '../../models/mpm-getrangeinfo-response copy';

@Component({
  selector: 'app-r-sheet',
  imports: [],
  templateUrl: './r-sheet.component.html',
  styleUrl: './r-sheet.component.css',
})
export class RSheetComponent {
  // ctor
  constructor(private mpmService: MPMDataService) {}

  async ngOnInit(): Promise<void> {
    let prodInfo: MPMGetProductInfoResponse = await this.mpmService.getProductInfo('3');
    console.log('prodInfo:', prodInfo);

    let rangeInfo: MPMGetRangeInfoResponse = await this.mpmService.getRangeInfo('3', '1');
    console.log('rangeInfo:', rangeInfo);
  }
}
