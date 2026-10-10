// TypeScript shapes of the API's DTOs (backend: RowCycle.Application/Dtos). Keep in sync by hand.

/** RFC 7807 error body returned by every failing API call. */
export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  correlationId?: string;
  /** Field name (camelCase) → messages, for 400 validation errors. */
  errors?: Record<string, string[]>;
}

/** The standard list shape: `{ items, page, pageSize, total }`. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

// Auth (F1)
export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

/** The refresh token is never here: it lives in an httpOnly cookie the browser handles. */
export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
}

export interface VerifyEmailRequest {
  userId: string;
  token: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export type Role = 'Member' | 'Moderator' | 'StoreManager' | 'Admin';

export interface MeResponse {
  id: string;
  email: string;
  emailVerified: boolean;
  fullName: string;
  photoUrl: string | null;
  phone: string | null;
  pointsBalance: number;
  roles: Role[];
}

// Points (F2)
export type PointsEntryType = 'Earn' | 'Redeem' | 'Reverse' | 'Adjust';
export type PointsSourceType = 'Submission' | 'Order' | 'Manual';

export interface PointsEntry {
  id: string;
  amount: number;
  type: PointsEntryType;
  sourceType: PointsSourceType;
  sourceId: string | null;
  reason: string | null;
  createdAt: string;
}

export interface PointsHistoryResponse extends PagedResult<PointsEntry> {
  balance: number;
}

// Admin (F8, FR-19)
export interface SettingsResponse {
  dailySubmissionLimit: number;
}

export type UpdateSettingsRequest = SettingsResponse;

export interface AuditLogEntry {
  id: string;
  userId: string | null;
  action: string;
  entity: string;
  entityId: string | null;
  data: unknown;
  createdAt: string;
}

export interface AuditLogQuery {
  entity?: string;
  page: number;
  pageSize: number;
}

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
