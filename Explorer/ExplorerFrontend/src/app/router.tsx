import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './layout'
import { AuthProvider } from '../features/auth/AuthContext'
import { ProtectedRoute } from '../features/auth/ProtectedRoute'
import { LoginPage } from '../pages/LoginPage'
import { RegisterPage } from '../pages/RegisterPage'
import { MyToursPage } from '../pages/MyToursPage'
import { CreateTourPage } from '../pages/CreateTourPage'
import { TourDetailsPage } from '../pages/TourDetailsPage'
import { SimulatorPage } from '../pages/SimulatorPage'
import { ToursCatalogPage } from '../pages/ToursCatalogPage'
import { ActiveTourPage } from '../pages/ActiveTourPage'
import { CartPage } from '../pages/CartPage'
import { PurchasedToursPage } from '../pages/PurchasedToursPage'

export function AppRouter() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/tours" replace />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

            <Route
              path="/tours"
              element={
                <ProtectedRoute>
                  <ToursCatalogPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/tours/me"
              element={
                <ProtectedRoute>
                  <MyToursPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/tours/new"
              element={
                <ProtectedRoute>
                  <CreateTourPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/tours/:id"
              element={
                <ProtectedRoute>
                  <TourDetailsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/simulator"
              element={
                <ProtectedRoute>
                  <SimulatorPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/tours/executions/:executionId"
              element={
                <ProtectedRoute>
                  <ActiveTourPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/purchases/cart"
              element={
                <ProtectedRoute>
                  <CartPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/purchases/tokens"
              element={
                <ProtectedRoute>
                  <PurchasedToursPage />
                </ProtectedRoute>
              }
            />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
