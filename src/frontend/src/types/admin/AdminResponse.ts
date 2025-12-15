export interface PagedResult<T> {
  items: T[];
  total: number;
  skip: number;
  take: number;
}
