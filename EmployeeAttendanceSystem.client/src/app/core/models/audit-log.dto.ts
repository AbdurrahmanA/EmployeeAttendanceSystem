export interface AuditLogDto {
  Id: number;
  UserId?: string;
  UserName?: string;
  EmployeeName?: string;
  Action: string;
  EntityType?: string;
  EntityId?: string;
  Timestamp: string;
  Success: boolean;
  IpAddress?: string;
  ErrorMessage?: string;
  OldValuesPreview?: string;
  NewValuesPreview?: string;
  OldValues?: string;
  NewValues?: string;
  UserAgent?: string;
}


export interface PaginatedResult<T> {
  data: T[];      
  totalCount: number;
  pageSize: number; 
  currentPage: number; 
  totalPages: number;  
}