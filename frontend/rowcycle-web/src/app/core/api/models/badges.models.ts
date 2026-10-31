// Badges (F4). TypeScript shapes of the API's DTOs (backend: RowCycle.Application/Dtos). Keep in sync by hand.

// Badges (F4)
export type BadgeRuleType = 'TotalApproved' | 'CategoryApproved' | 'FirstOrder';

export interface Badge {
  id: string;
  name: string;
  /** A Material icon name from GET /admin/badges/icons. */
  icon: string;
  ruleType: BadgeRuleType;
  ruleCategoryId: string | null;
  ruleCategoryName: string | null;
  threshold: number;
}

export interface SaveBadgeRequest {
  name: string;
  icon: string;
  ruleType: BadgeRuleType;
  ruleCategoryId: string | null;
  threshold: number;
}

export interface MyBadge {
  id: string;
  name: string;
  icon: string;
  awardedAt: string;
}
