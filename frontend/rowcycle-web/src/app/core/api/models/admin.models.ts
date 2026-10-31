// Admin: settings, audit log, users, dashboard (F8, FR-19). TypeScript shapes of the API's DTOs (backend: RowCycle.Application/Dtos). Keep in sync by hand.
import { Role } from './auth.models';

// Admin (F8, FR-19)
export interface SettingsResponse {
  dailySubmissionLimit: number;
  lowStockThreshold: number;
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

// Admin users (F8)
export interface AdminUserListItem {
  id: string;
  fullName: string;
  email: string;
  roles: Role[];
  isActive: boolean;
  emailVerified: boolean;
  pointsBalance: number;
}

export interface AdminUserDetail extends AdminUserListItem {
  phone: string | null;
  approvedSubmissions: number;
  pendingSubmissions: number;
  orders: number;
  badges: number;
}

export interface UserQuery {
  search?: string;
  role?: Role | '';
  blocked?: boolean | null;
  page: number;
  pageSize: number;
}

export interface PointsChange {
  entryId: string;
  amount: number;
  balance: number;
}

// Dashboard (F8)
export interface LowStockItem {
  productId: string;
  productName: string;
  variantId: string;
  variantName: string;
  sku: string;
  stock: number;
}

export interface Dashboard {
  pendingSubmissions: number;
  newOrders: number;
  lowStockThreshold: number;
  lowStockCount: number;
  lowStock: LowStockItem[];
}
