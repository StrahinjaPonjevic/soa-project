<?php

namespace App\Http\Controllers;

use App\Http\Requests\AddToCartRequest;
use App\Models\ShoppingCart;
use App\Services\CartService;
use App\Services\CheckoutSagaService;
use App\Services\PurchaseOperationException;
use App\Services\TourCatalogException;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class CartController extends Controller
{
    public function __construct(
        private readonly CartService $cartService,
        private readonly CheckoutSagaService $checkoutSagaService
    ) {
    }

    public function show(Request $request): JsonResponse
    {
        $touristId = $this->touristId($request);
        $cart = $this->cartService->getCart($touristId);
        return response()->json($this->toCartResponse($cart));
    }

    public function addItem(AddToCartRequest $request): JsonResponse
    {
        try {
            $touristId = $this->touristId($request);
            $cart = $this->cartService->addItem(
                $touristId,
                (int) $request->input('tourId'),
                (string) $request->header('Authorization')
            );

            return response()->json($this->toCartResponse($cart), 201);
        } catch (TourCatalogException|PurchaseOperationException $exception) {
            return response()->json(['message' => $exception->getMessage()], $exception->statusCode());
        }
    }

    public function removeItem(Request $request, int $tourId): JsonResponse
    {
        try {
            $touristId = $this->touristId($request);
            $cart = $this->cartService->removeItem($touristId, $tourId);
            return response()->json($this->toCartResponse($cart));
        } catch (PurchaseOperationException $exception) {
            return response()->json(['message' => $exception->getMessage()], $exception->statusCode());
        }
    }

    public function checkout(Request $request): JsonResponse
    {
        try {
            $tokens = $this->checkoutSagaService->checkout(
                $this->touristId($request),
                (string) $request->header('Authorization')
            );

            return response()->json([
                'tokens' => $tokens->map(static fn ($token) => [
                    'tourId' => $token->tour_id,
                    'token' => $token->token,
                    'purchasedAt' => $token->purchased_at?->toIso8601String(),
                ])->values(),
            ], 201);
        } catch (TourCatalogException|PurchaseOperationException $exception) {
            return response()->json(['message' => $exception->getMessage()], $exception->statusCode());
        }
    }

    private function touristId(Request $request): int
    {
        return (int) $request->attributes->get('auth.user_id');
    }

    private function toCartResponse(ShoppingCart $cart): array
    {
        return [
            'touristId' => $cart->tourist_id,
            'totalPrice' => (float) $cart->total_price,
            'items' => $cart->items->map(static fn ($item) => [
                'tourId' => $item->tour_id,
                'tourName' => $item->tour_name,
                'price' => (float) $item->price,
            ])->values(),
        ];
    }
}
