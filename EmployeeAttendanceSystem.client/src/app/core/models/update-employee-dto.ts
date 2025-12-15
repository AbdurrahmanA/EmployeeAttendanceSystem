export interface UpdateEmployeeDto {
  Email: string;
  Name: string;
  Surname: string;
  Department?: string;
  Password?: string; 
}

export interface EmployeeDto {
  Id: string;
  Email: string;
  Name: string;
  Surname: string;
  Department?: string;
}