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
  latitude: number | null
  longitude: number | null
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
  signalWord: string | null
  registrationExpiryDate: string | null
  preHarvestIntervalDays: number | null
  reEntryIntervalHours: number | null
  maxApplicationRate: number | null
  maxApplicationRateUnit: string | null
  maxWindSpeedMetersPerSecond: number | null
  labelNotes: string | null
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

export interface FarmPermissionsResponse {
  farmId: string
  permissions: string[]
}

export interface FieldDashboardSummary {
  total: number
  active: number
  totalAreaHectares: number
}

export interface CropDashboardSummary {
  totalAssignments: number
  planned: number
  planted: number
  harvested: number
  cancelled: number
}

export interface WorkerDashboardSummary {
  total: number
  active: number
  activeApplicators: number
  expiredCertifications: number
  certificationsExpiringWithin30Days: number
}

export interface InventoryDashboardSummary {
  stockLineCount: number
  productCount: number
  batchCount: number
  expiredBatchCount: number
  batchesExpiringWithin30Days: number
}

export interface ApplicationDashboardSummary {
  total: number
  draft: number
  completed: number
  completedWithin30Days: number
  areaTreatedWithin30DaysHectares: number
}

export interface FarmDashboard {
  farmId: string
  farmName: string
  farmCode: string
  location: string | null
  generatedAtUtc: string
  visibleSections: string[]
  fields: FieldDashboardSummary | null
  crops: CropDashboardSummary | null
  workers: WorkerDashboardSummary | null
  inventory: InventoryDashboardSummary | null
  applications: ApplicationDashboardSummary | null
  recentApplications: Array<{
    id: string
    applicationDateUtc: string
    fieldName: string
    cropName: string
    workerName: string
    targetPest: string
    status: string
  }>
  recentInventoryTransactions: Array<{
    id: string
    createdAtUtc: string
    productName: string
    transactionType: string
    quantity: number
    batchNumber: string | null
    reference: string | null
  }>
}

export interface SystemDashboard {
  generatedAtUtc: string
  totalFarms: number
  activeFarms: number
  totalFields: number
  activeFields: number
  activeCropAssignments: number
  activeWorkers: number
  activeApplicators: number
  inventoryProductCount: number
  draftApplications: number
  completedApplications: number
  completedApplicationsWithin30Days: number
}

export interface ApplicationHistoryItem {
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
  areaTreatedHectares: number
  status: string
  productCount: number
  hasInventoryTraceability: boolean
  createdByUserId: string
  createdByDisplayName: string
  createdAtUtc: string
}

export interface ApplicationDetail extends PesticideApplication {
  createdByDisplayName: string
  createdByEmail: string | null
  weather: {
    id: string
    recordedAtUtc: string
    temperatureCelsius: number | null
    humidityPercentage: number | null
    rainfallMillimeters: number | null
    windSpeedMetersPerSecond: number | null
    windDirectionDegrees: number | null
    conditions: string | null
    source: string
  } | null
  products: Array<ApplicationProduct & {
    inventoryTransaction: {
      id: string
      transactionType: string
      quantity: number
      batchNumber: string | null
      expiryDate: string | null
      reference: string | null
      reason: string | null
      createdByUserId: string
      createdAtUtc: string
    } | null
  }>
}

export interface PagedReport<T> extends PagedResponse<T> {
  generatedAtUtc: string
}

export interface ApplicationReport extends PagedReport<{
  id: string
  applicationDateUtc: string
  fieldName: string
  cropName: string
  workerName: string
  targetPest: string
  applicationMethod: string
  areaTreatedHectares: number
  status: string
  productCount: number
}> {
  draftCount: number
  completedCount: number
  totalAreaTreatedHectares: number
}

export interface InventoryReport extends PagedReport<{
  id: string
  createdAtUtc: string
  productName: string
  transactionType: string
  quantity: number
  batchNumber: string | null
  expiryDate: string | null
  reference: string | null
  reason: string | null
}> {
  stockInCount: number
  stockOutCount: number
  adjustmentInCount: number
  adjustmentOutCount: number
}

export interface ProductUsageReport {
  generatedAtUtc: string
  totalProducts: number
  items: Array<{
    productId: string
    productName: string
    applicationCount: number
    totalQuantityUsed: number
    firstApplicationDateUtc: string | null
    lastApplicationDateUtc: string | null
  }>
}

export interface WorkerActivityReport {
  generatedAtUtc: string
  totalWorkers: number
  items: Array<{
    workerId: string
    workerName: string
    workerCode: string
    isActive: boolean
    applicationCount: number
    completedApplicationCount: number
    totalAreaTreatedHectares: number
    lastApplicationDateUtc: string | null
  }>
}

export interface ManagedUser {
  id: string
  displayName: string
  email: string | null
  emailConfirmed: boolean
  isActive: boolean
  isLockedOut: boolean
  lockoutEnd: string | null
  accessFailedCount: number
  createdAtUtc: string
  systemRoles: string[]
}

export interface AuditValueChange {
  oldValue: unknown
  newValue: unknown
}

export interface AuditLog {
  id: string
  occurredAtUtc: string
  actorUserId: string | null
  actorDisplayName: string | null
  actorEmail: string | null
  farmId: string | null
  action: string
  entityType: string
  entityId: string
  changes: Record<string, AuditValueChange>
  httpMethod: string | null
  requestPath: string | null
  ipAddress: string | null
  userAgent: string | null
}

export interface SafetyCheckItem {
  code: string
  severity: 'Pass' | 'Warning' | 'Violation' | string
  title: string
  message: string
}

export interface ApplicationSafetyCheck {
  applicationId: string
  isCompliant: boolean
  violationCount: number
  warningCount: number
  reEntryAllowedAtUtc: string | null
  earliestSafeHarvestDate: string | null
  checks: SafetyCheckItem[]
}

export interface WorkerCertificationStatusItem {
  workerId: string
  fullName: string
  workerCode: string
  certificationNumber: string | null
  certificationExpiryDate: string | null
  status: 'Valid' | 'ExpiringSoon' | 'Expired' | 'Missing' | string
  daysRemaining: number | null
}

export interface ReEntryHoldItem {
  applicationId: string
  fieldId: string
  fieldName: string
  cropName: string
  applicationDateUtc: string
  reEntryIntervalHours: number
  reEntryAllowedAtUtc: string
  products: string[]
}

export interface PreHarvestHoldItem {
  applicationId: string
  fieldId: string
  fieldName: string
  cropName: string
  applicationDate: string
  preHarvestIntervalDays: number
  earliestSafeHarvestDate: string
  expectedHarvestDate: string | null
  conflictsWithExpectedHarvest: boolean
  products: string[]
}

export interface ProductRegistrationAlertItem {
  productId: string
  productName: string
  registrationNumber: string | null
  registrationExpiryDate: string
  status: 'Expired' | 'ExpiringSoon' | string
  daysRemaining: number
}

export interface ComplianceOverview {
  generatedAtUtc: string
  openRecords: number
  nonCompliantRecords: number
  expiredCertifications: number
  expiringCertificationsWithin30Days: number
  activeReEntryHolds: number
  activePreHarvestHolds: number
  expiringProductRegistrations: number
  certifications: WorkerCertificationStatusItem[]
  reEntryHolds: ReEntryHoldItem[]
  preHarvestHolds: PreHarvestHoldItem[]
  productRegistrations: ProductRegistrationAlertItem[]
}

export type ComplianceRecordType =
  | 'Inspection'
  | 'Incident'
  | 'AuditFinding'
  | 'CertificationReview'
  | 'RegulatoryCheck'

export type ComplianceRecordStatus =
  | 'Open'
  | 'InReview'
  | 'Resolved'
  | 'NonCompliant'

export type ComplianceSeverity = 'Low' | 'Medium' | 'High' | 'Critical'

export interface ComplianceRecord {
  id: string
  farmId: string
  fieldId: string | null
  fieldName: string | null
  pesticideApplicationId: string | null
  workerId: string | null
  workerName: string | null
  recordType: ComplianceRecordType | string
  title: string
  status: ComplianceRecordStatus | string
  severity: ComplianceSeverity | string
  checkedAtUtc: string
  dueDateUtc: string | null
  resolvedAtUtc: string | null
  inspectorName: string | null
  findings: string | null
  correctiveAction: string | null
  createdByUserId: string
  createdByDisplayName: string
  createdAtUtc: string
}

export interface InventoryThreshold {
  id: string
  farmId: string
  productId: string
  productName: string
  minimumQuantity: number
  currentQuantityOnHand: number
  isLowStock: boolean
  updatedAtUtc: string
}

export type NotificationCategory =
  | 'LowStock'
  | 'ExpiringBatch'
  | 'ExpiredBatch'
  | 'ExpiringCertification'
  | 'ExpiredCertification'
  | 'ProductRegistration'
  | 'ReEntryHold'
  | 'PreHarvestConflict'
  | 'ComplianceAlert'

export type NotificationSeverity = 'Info' | 'Warning' | 'Critical'

export interface FarmNotification {
  id: string
  farmId: string
  category: NotificationCategory | string
  severity: NotificationSeverity | string
  title: string
  message: string
  actionPath: string | null
  isRead: boolean
  readAtUtc: string | null
  isResolved: boolean
  resolvedAtUtc: string | null
  createdAtUtc: string
  lastEvaluatedAtUtc: string
}

export interface NotificationListResponse extends PagedResponse<FarmNotification> {
  unreadCount: number
  criticalCount: number
}

export interface NotificationSummary {
  unreadCount: number
  criticalCount: number
}
