import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren,
} from 'react'
import { useQuery } from '@tanstack/react-query'
import { apiRequest } from '../lib/api'
import type { Farm } from '../types/api'
import { useAuth } from '../auth/AuthContext'

/* oxlint-disable react/only-export-components -- provider and hook form one public context API */

interface FarmContextValue {
  farms: Farm[]
  selectedFarm: Farm | null
  selectedFarmId: string | null
  setSelectedFarmId: (farmId: string) => void
  isLoading: boolean
  error: Error | null
}

const FarmContext = createContext<FarmContextValue | null>(null)
const storageKey = 'pestitrack.selectedFarmId'

export function FarmProvider({ children }: PropsWithChildren) {
  const { user } = useAuth()
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

  useEffect(() => {
    if (effectiveFarmId) localStorage.setItem(storageKey, effectiveFarmId)
  }, [effectiveFarmId])

  const setSelectedFarmId = (farmId: string) => {
    setSelectedFarmIdState(farmId)
    localStorage.setItem(storageKey, farmId)
  }

  const value = useMemo(
    () => ({
      farms,
      selectedFarm: farms.find((farm) => farm.id === effectiveFarmId) ?? null,
      selectedFarmId: effectiveFarmId,
      setSelectedFarmId,
      isLoading,
      error: error as Error | null,
    }),
    [farms, effectiveFarmId, isLoading, error],
  )

  return <FarmContext.Provider value={value}>{children}</FarmContext.Provider>
}

export function useFarm() {
  const context = useContext(FarmContext)
  if (!context) throw new Error('useFarm must be used inside FarmProvider')
  return context
}
