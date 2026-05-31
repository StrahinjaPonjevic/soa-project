<?php

use App\Http\Controllers\CartController;
use App\Http\Controllers\TokenController;
use Illuminate\Support\Facades\Route;

Route::prefix('purchases')
    ->middleware('jwt.auth')
    ->group(function (): void {
        Route::get('/cart', [CartController::class, 'show']);
        Route::post('/cart/items', [CartController::class, 'addItem']);
        Route::delete('/cart/items/{tourId}', [CartController::class, 'removeItem']);
        Route::post('/cart/checkout', [CartController::class, 'checkout']);

        Route::get('/tokens', [TokenController::class, 'index']);
        Route::get('/tokens/{tourId}/exists', [TokenController::class, 'exists']);
    });
