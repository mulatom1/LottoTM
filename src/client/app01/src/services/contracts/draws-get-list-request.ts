/**
 * Request parameters for GET /api/draws endpoint
 * Retrieves a paginated list of lottery draws with sorting options
 */
export interface DrawsGetListRequest {
  /**
   * Page number (starting from 1)
   * Default: 1
   * Validation: >= 1
   */
  page?: number;

  /**
   * Number of items per page
   * Default: 20
   * Validation: 1-100
   */
  pageSize?: number;

  /**
   * Field to sort by
   * Default: "drawDate"
   * Allowed values: "drawDate", "createdAt"
   */
  sortBy?: string;

  /**
   * Sort order direction
   * Default: "desc"
   * Allowed values: "asc", "desc"
   */
  sortOrder?: string;
}
