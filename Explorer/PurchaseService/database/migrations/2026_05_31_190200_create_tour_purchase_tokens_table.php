<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration {
    public function up(): void
    {
        Schema::create('tour_purchase_tokens', function (Blueprint $table): void {
            $table->id();
            $table->unsignedBigInteger('tourist_id');
            $table->unsignedBigInteger('tour_id');
            $table->uuid('token')->unique();
            $table->timestamp('purchased_at');
            $table->timestamps();

            $table->unique(['tourist_id', 'tour_id']);
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('tour_purchase_tokens');
    }
};
