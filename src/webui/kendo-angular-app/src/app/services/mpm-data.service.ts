import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from '../settings/app.settings';
import { MPMGetProductInfoResponse } from '../models/mpm-getproductinfo-response';
import { firstValueFrom } from 'rxjs/internal/firstValueFrom';
import { MPMGetRangeInfoResponse } from '../models/mpm-getrangeinfo-response copy';

@Injectable({
  providedIn: 'root',
})
export class MPMDataService {
  constructor(private http: HttpClient) {}

  // Get the Products info
  async getProductInfo(fileId: string): Promise<MPMGetProductInfoResponse> {
    let result = await firstValueFrom(this.http.get(`${API_URL}/mpm/mfile/${fileId}`));
    console.log(result);
    let resp: MPMGetProductInfoResponse = result as MPMGetProductInfoResponse;
    console.log('resp:', resp);
    return resp;
  }

  // Get the Range info
  async getRangeInfo(fileId: string, rangeId: string): Promise<MPMGetRangeInfoResponse> {
    let result = await firstValueFrom(this.http.get(`${API_URL}/mpm/mfile/${fileId}/range/${rangeId}`));
    console.log(result);
    let resp: MPMGetRangeInfoResponse = result as MPMGetRangeInfoResponse;
    console.log('resp:', resp);
    return resp;
  }

  // Functions for maintaining data locally till API save.
  // Save could be called after every change or batched.
  // Initially better to test with instant save for consistency & immediate
  // failure notifications.
  // Ranges
  async addRange() {}
  async removeRange() {}
  //
  async addRangeField() {}
  async removeRangeField() {}
  //
  async updateRangeField() {} // NOTE: any updates to removed fields must be removed before save
  // Series
  async addSeries() {}
  async removeSeries() {}
  //
  async addSeriesField() {}
  async removeSeriesField() {}
  async updateSeriesField() {}
  //
  async addSeriesDetail() {}
  async removeSeriesDetail() {}
  async upateSeriesDetail() {}

  // Save file, will make the required API calls to push changes to server
  async saveFile() {}
}
