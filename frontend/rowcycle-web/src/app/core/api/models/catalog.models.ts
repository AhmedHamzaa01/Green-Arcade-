// Store catalog, public and admin (F5, FR-13). TypeScript shapes of the API's DTOs (backend: RowCycle.Application/Dtos). Keep in sync by hand.

// Catalog (F5, FR-13)
export interface ProductCategory {
  id: string;
  name: string;
  slug: string;
  sortOrder: number;
  isActive: boolean;
}

export type ProductSort = 'Newest' | 'PriceAsc' | 'PriceDesc';

export interface ProductQuery {
  category?: string;
  search?: string;
  sort: ProductSort;
  page: number;
  pageSize: number;
}

export interface ProductListItem {
  id: string;
  slug: string;
  name: string;
  categoryName: string;
  categorySlug: string;
  priceEgp: number;
  pricePoints: number | null;
  rewardPoints: number;
  imageUrl: string | null;
  inStock: boolean;
}

export interface ProductImage {
  id: string;
  url: string;
  sortOrder: number;
}

export interface ProductVariant {
  id: string;
  name: string;
  priceEgp: number;
  inStock: boolean;
}

export interface ProductDetail {
  id: string;
  slug: string;
  name: string;
  description: string;
  categoryName: string;
  categorySlug: string;
  priceEgp: number;
  pricePoints: number | null;
  rewardPoints: number;
  inStock: boolean;
  variants: ProductVariant[];
  images: ProductImage[];
}

// Catalog admin (StoreManager, Admin)
export interface SaveProductCategoryRequest {
  name: string;
  slug: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface AdminProductQuery {
  search?: string;
  categoryId?: string;
  page: number;
  pageSize: number;
}

export interface AdminProductListItem {
  id: string;
  slug: string;
  name: string;
  categoryName: string;
  priceEgp: number;
  pricePoints: number | null;
  rewardPoints: number;
  isActive: boolean;
  totalStock: number;
  variantCount: number;
  imageUrl: string | null;
  createdAt: string;
}

export interface AdminProductVariant {
  id: string;
  name: string;
  sku: string;
  stock: number;
  priceOverride: number | null;
}

export interface AdminProduct {
  id: string;
  categoryId: string;
  categoryName: string;
  slug: string;
  name: string;
  description: string;
  priceEgp: number;
  pricePoints: number | null;
  rewardPoints: number;
  isActive: boolean;
  createdAt: string;
  variants: AdminProductVariant[];
  images: ProductImage[];
}

export interface SaveProductRequest {
  categoryId: string;
  name: string;
  slug: string | null;
  description: string;
  priceEgp: number;
  pricePoints: number | null;
  rewardPoints: number;
  isActive: boolean;
}

export interface SaveProductVariantRequest {
  name: string;
  sku: string;
  stock: number;
  priceOverride: number | null;
}
