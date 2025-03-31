// Get Product info response classes

export class MPMGetProductInfoResponse {
  code: number;
  message: string;
  products: MPMProductInfo[];
}

export class MPMProductInfo {
  productId: number;
  productName: string;
  productTypeInfo: MPMProductTypeInfo[];
}

export class MPMProductTypeInfo {
  productTypeId: number;
  productTypeName: string;
  rangeInfo: MPMRangeInfo[];
}
export class MPMRangeInfo {
  rangeId: number;
  rangeName: string;
  imageUrl: string;
}
