<?php

namespace App\Http\Controllers;

use App\Models\TourPurchaseToken;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class TokenController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $touristId = (int) $request->attributes->get('auth.user_id');

        $tokens = TourPurchaseToken::query()
            ->where('tourist_id', $touristId)
            ->orderByDesc('purchased_at')
            ->get()
            ->map(static fn (TourPurchaseToken $token) => [
                'tourId' => $token->tour_id,
                'token' => $token->token,
                'purchasedAt' => $token->purchased_at?->toIso8601String(),
            ])
            ->values();

        return response()->json(['tokens' => $tokens]);
    }

    public function exists(Request $request, int $tourId): JsonResponse
    {
        $touristId = (int) $request->attributes->get('auth.user_id');

        $exists = TourPurchaseToken::query()
            ->where('tourist_id', $touristId)
            ->where('tour_id', $tourId)
            ->exists();

        return response()->json([
            'tourId' => $tourId,
            'purchased' => $exists,
        ]);
    }
}
