<?php

namespace App\Services;

use App\Models\ShoppingCart;
use App\Models\TourPurchaseToken;
use Illuminate\Support\Carbon;
use Illuminate\Support\Collection;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Str;

class CheckoutSagaService
{
    public function __construct(private readonly TourCatalogClient $tourCatalogClient)
    {
    }

    /**
     * @return Collection<int, TourPurchaseToken>
     */
    public function checkout(int $touristId, string $authorization): Collection
    {
        $cart = ShoppingCart::query()
            ->where('tourist_id', $touristId)
            ->with('items')
            ->first();

        if (!$cart || $cart->items->isEmpty()) {
            throw new PurchaseOperationException(409, 'Cart is empty.');
        }

        // SAGA step 1: Re-validate each tour against TourService.
        foreach ($cart->items as $item) {
            $snapshot = $this->tourCatalogClient->getTourSnapshot($item->tour_id, $authorization);
            if ($snapshot['status'] !== 'Published') {
                throw new PurchaseOperationException(
                    409,
                    "Tour {$item->tour_id} is no longer purchasable."
                );
            }
        }

        // SAGA step 2 + 3 (local transaction):
        // create tokens and clear cart atomically.
        return DB::transaction(function () use ($touristId, $cart): Collection {
            $lockedCart = ShoppingCart::query()
                ->where('id', $cart->id)
                ->lockForUpdate()
                ->with('items')
                ->first();

            if (!$lockedCart || $lockedCart->items->isEmpty()) {
                throw new PurchaseOperationException(409, 'Cart is empty.');
            }

            $createdTokens = collect();
            foreach ($lockedCart->items as $item) {
                $existing = TourPurchaseToken::query()
                    ->where('tourist_id', $touristId)
                    ->where('tour_id', $item->tour_id)
                    ->first();

                if ($existing) {
                    throw new PurchaseOperationException(
                        409,
                        "Tour {$item->tour_id} is already purchased."
                    );
                }

                $createdTokens->push(TourPurchaseToken::query()->create([
                    'tourist_id' => $touristId,
                    'tour_id' => $item->tour_id,
                    'token' => (string) Str::uuid(),
                    'purchased_at' => Carbon::now('UTC'),
                ]));
            }

            $lockedCart->items()->delete();
            $lockedCart->total_price = 0;
            $lockedCart->save();

            return $createdTokens;
        });
    }
}
