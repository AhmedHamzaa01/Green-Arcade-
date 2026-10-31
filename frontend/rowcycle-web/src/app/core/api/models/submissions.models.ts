// Photo-verified actions and their categories (F3). TypeScript shapes of the API's DTOs (backend: RowCycle.Application/Dtos). Keep in sync by hand.

// Submissions (F3)
export type SubmissionStatus = 'Pending' | 'Approved' | 'Rejected';

export interface SubmissionCategory {
  id: string;
  name: string;
  pointsReward: number;
  tracksWeight: boolean;
}

export interface Submission {
  id: string;
  userId: string;
  memberName: string;
  categoryId: string;
  categoryName: string;
  categoryPoints: number;
  photoUrl: string;
  note: string | null;
  weightKg: number | null;
  status: SubmissionStatus;
  reviewedBy: string | null;
  reviewedAt: string | null;
  rejectReason: string | null;
  createdAt: string;
}

export interface CreateSubmissionRequest {
  categoryId: string;
  note: string | null;
  weightKg: number | null;
}

export interface AdminSubmissionCategory extends SubmissionCategory {
  isActive: boolean;
}

export interface SaveSubmissionCategoryRequest {
  name: string;
  pointsReward: number;
  tracksWeight: boolean;
  isActive: boolean;
}
