// Shapes shared by every API: errors and paged lists. TypeScript shapes of the API's DTOs (backend: RowCycle.Application/Dtos). Keep in sync by hand.

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
