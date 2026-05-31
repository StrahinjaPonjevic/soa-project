<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class TourPurchaseToken extends Model
{
    protected $fillable = [
        'tourist_id',
        'tour_id',
        'token',
        'purchased_at',
    ];

    protected $casts = [
        'purchased_at' => 'datetime',
    ];
}
