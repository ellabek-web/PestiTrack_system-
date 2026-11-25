import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren,
} from 'react'
import { useQuery } from '@tanstack/react-query'
import { apiRequest } from '../lib/api'
import type { Farm, FarmPermissionsResponse } from '../types/api'
import { useAuth } from '../auth/AuthContext'

/* oxlint-disable react/only-export-components -- provider and hook form one public context API */

interface FarmContextValue {
  farms: Farm[]
  selectedFarm: Farm | null
  selectedFarmId: string | null
  setSelectedFarmId: (farmId: string) => void
  permissions: string[]
  permissionsLoading: boolean
  hasPermission: (permissionCode: string) => boolean
  isLoading: boolean
  error: Error | null
}

const FarmContext = createContext<FarmContextValue | null>(null)
const storageKey = 'pestitrack.selectedFarmId'

export function FarmProvider({ children }: PropsWithChildren) {
  const { user, isAdministrator } = useAuth()
  const [selectedFarmId, setSelectedFarmIdState] = useState<string | null>(
    () => localStorage.getItem(storageKey),
  )
  const { data: farms = [], isLoading, error } = useQuery({
    queryKey: ['farms'],
    queryFn: () => apiRequest<Farm[]>('/api/farms'),
    enabled: Boolean(user),
    retry: false,
  })

  const effectiveFarmId =
    farms.find((farm) => farm.id === selectedFarmId)?.id ?? farms[0]?.id ?? null

  const permissionsQuery = useQuery({
    queryKey: ['farm-permissions', effectiveFarmId],
    queryFn: () =>
      apiRequest<FarmPermissionsResponse>(`/api/farms/${effectiveFarmId}/permissions`),
    enabled: Boolean(user && effectiveFarmId),
    retry: false,
    staleTime: 60_000,
  })

  useEffect(() => {
    if (effectiveFarmId) localStorage.setItem(storageKey, effectiveFarmId)
  }, [effectiveFarmId])

  const setSelectedFarmId = (farmId: string) => {
    setSelectedFarmIdState(farmId)
    localStorage.setItem(storageKey, farmId)
  }

  const permissions = useMemo(
    () => permissionsQuery.data?.permissions ?? [],
    [permissionsQuery.data],
  )
  const permissionSet = useMemo(() => new Set(permissions), [permissions])

  const hasPermission = useCallback(
    (permissionCode: string) => isAdministrator || permissionSet.has(permissionCode),
    [isAdministrator, permissionSet],
  )

  const value = useMemo(
    () => ({
      farms,
      selectedFarm: farms.find((farm) => farm.id === effectiveFarmId) ?? null,
      selectedFarmId: effectiveFarmId,
      setSelectedFarmId,
      permissions,
      permissionsLoading: permissionsQuery.isLoading,
      hasPermission,
      isLoading,
      error: error as Error | null,
    }),
    [
      farms,
      effectiveFarmId,
      permissions,
      permissionsQuery.isLoading,
      hasPermission,
      isLoading,
      error,
    ],
  )

  return <FarmContext.Provider value={value}>{children}</FarmContext.Provider>
}

export function useFarm() {
  const context = useContext(FarmContext)
  if (!context) throw new Error('useFarm must be used inside FarmProvider')
  return context
}
