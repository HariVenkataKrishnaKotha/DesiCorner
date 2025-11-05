export interface ApiResponse<T = any> {
  isSuccess: boolean;
  message: string;
  result?: T;
}

export interface PaginatedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
}