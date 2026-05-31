<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration {
    public function up(): void
    {
        Schema::create('order_items', function (Blueprint $table): void {
            $table->id();
            $table->foreignId('shopping_cart_id')
                ->constrained('shopping_carts')
                ->cascadeOnDelete();
            $table->unsignedBigInteger('tour_id');
            $table->string('tour_name', 200);
            $table->decimal('price', 10, 2);
            $table->timestamps();

            $table->unique(['shopping_cart_id', 'tour_id']);
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('order_items');
    }
};
