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
