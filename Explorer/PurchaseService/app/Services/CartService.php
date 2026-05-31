<?php

namespace App\Services;

use App\Models\OrderItem;
use App\Models\ShoppingCart;
use Illuminate\Database\Eloquent\Builder;
use Illuminate\Support\Facades\DB;

class CartService
{
    public function __construct(private readonly TourCatalogClient $tourCatalogClient)
    {
    }

    public function getOrCreateCart(int $touristId): ShoppingCart
    {
        return ShoppingCart::query()->firstOrCreate(
            ['tourist_id' => $touristId],
            ['total_price' => 0]
        );
    }

    public function getCart(int $touristId): ShoppingCart
    {
        $cart = $this->getOrCreateCart($touristId);
        $cart->load('items');
        return $cart;
    }

    public function addItem(int $touristId, int $tourId, string $tourName, float $tourPrice, string $tourStatus): ShoppingCart
    {
        $snapshot = ['id' => $tourId, 'name' => $tourName, 'price' => $tourPrice, 'status' => $tourStatus];

        if ($snapshot['status'] !== 'Published') {
            throw new PurchaseOperationException(409, 'Only published tours can be added to cart.');
        }

        $alreadyPurchased = \App\Models\TourPurchaseToken::query()
            ->where('tourist_id', $touristId)
            ->where('tour_id', $tourId)
            ->exists();
        if ($alreadyPurchased) {
            throw new PurchaseOperationException(409, 'Tour is already purchased.');
        }

        return DB::transaction(function () use ($touristId, $tourId, $snapshot): ShoppingCart {
            /** @var ShoppingCart $cart */
            $cart = ShoppingCart::query()
                ->lockForUpdate()
                ->firstOrCreate(['tourist_id' => $touristId], ['total_price' => 0]);

            $itemExists = $cart->items()->where('tour_id', $tourId)->exists();
            if ($itemExists) {
                throw new PurchaseOperationException(409, 'Tour is already in cart.');
            }

            $cart->items()->create([
                'tour_id' => $tourId,
                'tour_name' => $snapshot['name'],
                'price' => $snapshot['price'],
            ]);

            $this->recalculateTotal($cart);
            $cart->load('items');

            return $cart;
        });
    }

    public function removeItem(int $touristId, int $tourId): ShoppingCart
    {
        return DB::transaction(function () use ($touristId, $tourId): ShoppingCart {
            /** @var ShoppingCart|null $cart */
            $cart = ShoppingCart::query()
                ->where('tourist_id', $touristId)
                ->lockForUpdate()
                ->first();

            if (!$cart) {
                throw new PurchaseOperationException(404, 'Cart not found.');
            }

            $deleted = $cart->items()
                ->where('tour_id', $tourId)
                ->delete();

            if ($deleted === 0) {
                throw new PurchaseOperationException(404, 'Cart item not found.');
            }

            $this->recalculateTotal($cart);
            $cart->load('items');

            return $cart;
        });
    }

    public function recalculateTotal(ShoppingCart $cart): void
    {
        $total = $cart->items()->sum('price');
        $cart->total_price = (float) $total;
        $cart->save();
    }
}
