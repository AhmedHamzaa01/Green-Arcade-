// Auth and the signed-in user (F1). TypeScript shapes of the API's DTOs (backend: RowCycle.Application/Dtos). Keep in sync by hand.

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
