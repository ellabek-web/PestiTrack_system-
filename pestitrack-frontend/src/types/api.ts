export interface User {
  id: string
  email: string | null
  displayName: string
  systemRoles: string[]
}

export interface AuthResponse {
  user: User
}

export interface LoginResponse extends AuthResponse {
  message: string
}

export interface Farm {
  id: string
  name: string
  code: string
  location: string | null
  isActive: boolean
  createdAtUtc: string
}

export interface Field {
  id: string
  farmId: string
  name: string
  code: string
  areaHectares: number | null
  description: string | null
  isActive: boolean
  createdAtUtc: string
}

export interface Crop {
  id: string
  name: string
  scientificName: string | null
  description: string | null
  isActive: boolean
  createdAtUtc: string
}

export type FieldCropStatus = 'Planned' | 'Planted' | 'Harvested' | 'Cancelled'

export interface FieldCrop {
  id: string
  farmId: string
  fieldId: string
  cropId: string
  cropName: string
  plantingDate: string
  expectedHarvestDate: string | null
  actualHarvestDate: string | null
  areaHectares: number | null
  status: FieldCropStatus | number
  createdAtUtc: string
}

export interface Product {
  id: string
  name: string
  activeIngredient: string | null
  manufacturer: string | null
  registrationNumber: string | null
  formulation: string | null
  concentration: string | null
  hazardClass: string | null
  description: string | null
  isActive: boolean
  createdAtUtc: string
}

export interface InventoryBalance {
  farmId: string
  productId: string
  productName: string
  batchNumber: string
  expiryDate: string | null
  quantityOnHand: number
}

export type InventoryTransactionType =
  | 'StockIn'
  | 'StockOut'
  | 'AdjustmentIn'
  | 'AdjustmentOut'

export interface InventoryTransaction {
  id: string
  farmId: string
  productId: string
  productName: string
  transactionType: InventoryTransactionType
  quantity: number
  batchNumber: string | null
  expiryDate: string | null
  reference: string | null
  reason: string | null
  notes: string | null
  createdByUserId: string
  createdAtUtc: string
}

export interface PagedResponse<T> {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  items: T[]
}

export interface Worker {
  id: string
  farmId: string
  fullName: string
  workerCode: string
  phoneNumber: string | null
  email: string | null
  isApplicator: boolean
  certificationNumber: string | null
  certificationExpiryDate: string | null
  isActive: boolean
  createdAtUtc: string
}

export interface WeatherRecord {
  id: string
  farmId: string
  fieldId: string | null
  recordedAtUtc: string
  temperatureCelsius: number | null
  humidityPercentage: number | null
  rainfallMillimeters: number | null
  windSpeedMetersPerSecond: number | null
  windDirectionDegrees: number | null
  conditions: string | null
  source: string
  notes: string | null
  createdAtUtc: string
}

export interface ApplicationProduct {
  id: string
  productId: string
  productName: string
  batchNumber: string
  quantityUsed: number
  applicationRate: number
  rateUnit: string
  inventoryTransactionId: string | null
}

export interface PesticideApplication {
  id: string
  farmId: string
  fieldId: string
  fieldName: string
  fieldCropId: string
  cropName: string
  workerId: string
  workerName: string
  applicationDateUtc: string
  targetPest: string
  applicationMethod: string
  equipment: string | null
  areaTreatedHectares: number
  weatherRecordId: string | null
  notes: string | null
  status: 'Draft' | 'Completed'
  createdByUserId: string
  createdAtUtc: string
  products: ApplicationProduct[]
}

export interface FarmRole {
  id: string
  farmId: string
  name: string
  description: string | null
  isSystemRole: boolean
  permissionCodes: string[]
}

export interface FarmUserRole {
  id: string
  name: string
}

export interface FarmUser {
  id: string
  farmId: string
  userId: string
  displayName: string
  email: string | null
  isActive: boolean
  assignedAtUtc: string
  roles: FarmUserRole[]
}

export interface ApiMessage {
  message: string
}
